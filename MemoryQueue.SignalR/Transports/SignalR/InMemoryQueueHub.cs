using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MemoryQueue.SignalR.Transports.SignalR
{
    public class InMemoryQueueHub : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_SIGNALR_REQUEST_CANCELLED = "Request cancelled and client is being removed";
        private const string LOGMSG_SIGNALR_ACK_FAILED = "Failed to ack the message (Request was cancelled or client disconnected)";

        private readonly InMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<InMemoryQueueHub> _logger;

        public InMemoryQueueHub(InMemoryQueueManager inMemoryQueueManager, ILoggerFactory loggerFactory)
        {
            _queueManager = inMemoryQueueManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<InMemoryQueueHub>();
        }

        public async Task QueueInfo(string? queue)
        {
            var inMemoryQueue = _queueManager.GetOrCreateQueue(queue);
            int mainQueueSize = inMemoryQueue.MainChannelCount;
            int retryQueueSize = inMemoryQueue.RetryChannelCount;

            var reply = new QueueInfoReply()
            {
                QueueName = inMemoryQueue.Name,
                QueueSize = mainQueueSize + retryQueueSize,
                MainQueueSize = mainQueueSize,
                RetryQueueSize = retryQueueSize,

                ConcurrentConsumers = inMemoryQueue.ConsumersCount,

                AckCounter = inMemoryQueue.Counters.AckCounter,
                AckPerSecond = inMemoryQueue.Counters.AckPerSecond,

                NackCounter = inMemoryQueue.Counters.NackCounter,
                NackPerSecond = inMemoryQueue.Counters.NackPerSecond,

                PubCounter = inMemoryQueue.Counters.PubCounter,
                PubPerSecond = inMemoryQueue.Counters.PubPerSecond,

                RedeliverCounter = inMemoryQueue.Counters.RedeliverCounter,
                RedeliverPerSecond = inMemoryQueue.Counters.RedeliverPerSecond,

                DeliverCounter = inMemoryQueue.Counters.DeliverCounter,
                DeliverPerSecond = inMemoryQueue.Counters.DeliverPerSecond,

                AvgAckTimeMilliseconds = inMemoryQueue.Counters.AvgConsumptionMs
            };
            reply.Consumers.AddRange(inMemoryQueue.Consumers.Select(x => ToSignalR(x)));

            await Clients.Caller.SendAsync(nameof(QueueInfoReply), reply);
        }

        private ConsumerInfoReply ToSignalR(QueueConsumerInfo info)
        {
            var _consumerInfo = new ConsumerInfoReply();
            _consumerInfo.Counters ??= new ConsumerCounters();

            _consumerInfo.Host = info.Host;
            _consumerInfo.Id = info.Id;
            _consumerInfo.Ip = info.Ip;
            _consumerInfo.Name = info.Name;
            _consumerInfo.Type = info.ConsumerType.ToString();

            _consumerInfo.Counters.AckCounter = info.Counters?.AckCounter ?? 0;
            _consumerInfo.Counters.AckPerSecond = info.Counters?.AckPerSecond ?? 0;
            _consumerInfo.Counters.AvgConsumptionMs = info.Counters?.AvgConsumptionMs ?? 0;
            _consumerInfo.Counters.DeliverCounter = info.Counters?.DeliverCounter ?? 0;
            _consumerInfo.Counters.DeliverPerSecond = info.Counters?.DeliverPerSecond ?? 0;
            _consumerInfo.Counters.NackCounter = info.Counters?.NackCounter ?? 0;
            _consumerInfo.Counters.NackPerSecond = info.Counters?.NackPerSecond ?? 0;
            _consumerInfo.Counters.Throttled = info.Counters?.Throttled ?? false;

            return _consumerInfo;
        }

        public async Task Publish(string item, string? queue)
        {
            await _queueManager
                    .GetOrCreateQueue(queue)
                    .EnqueueAsync(item).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<QueueItemReply> Consume(string clientName, string? queue, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            #region Setup
            var memoryQueue = (InMemoryQueue)_queueManager.GetOrCreateQueue(queue);
            var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.SignalR)
            {
                Id = Guid.NewGuid().ToString(),
                Host = Context.ConnectionId,
                Ip = Context.ConnectionId,
                Name = clientName ?? "Unknown"
            };
            var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.Name, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));

            Acker = new TaskCompletionSource<bool>();
            Acker.SetCanceled();

            QueueItem? currentItem = default;
            SemaphoreSlim semaphoreSlim = new (0);
            using var registration = cancellationToken.Register(() =>
            {
                using (semaphoreSlim)
                {
                    semaphoreSlim.Release();
                }
                logger.LogInformation(LOGMSG_SIGNALR_REQUEST_CANCELLED);
            });
            #endregion

            Debug.Assert(Acker.Task.IsCanceled);
            Debug.Assert(semaphoreSlim.CurrentCount == 0);
            Debug.Assert(currentItem is null);

            using var reader = memoryQueue.AddQueueReader(
                    consumerQueueInfo,
                    item => WriteAndAckAsync(item, ref currentItem, semaphoreSlim),
                    cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    break;
                }

                if (currentItem.HasValue)
                {
                    Debug.Assert(!Acker.Task.IsCompleted);
                    yield return new QueueItemReply()
                    {
                        Message = currentItem.Value.Message,
                        RetryCount = currentItem.Value.RetryCount,
                        Retrying = currentItem.Value.Retrying
                    };
                    currentItem = null;
                }
            }
            if (!Acker.Task.IsCompleted)
            {
                logger.LogWarning(LOGMSG_SIGNALR_ACK_FAILED);
            }

            Acker.TrySetResult(false);
            await reader.Completed.ConfigureAwait(false);
            memoryQueue.RemoveReader(reader);

            //_logger.LogCritical("SHOUD BE FINISHED");
        }

        private Task<bool> WriteAndAckAsync(QueueItem item, ref QueueItem? refItem, SemaphoreSlim semaphore)
        {
            try
            {
                refItem = item;
                return (Acker = new TaskCompletionSource<bool>()).Task;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public Task Ack(bool acked)
        {
            if (Acker.Task.IsCompleted)
            {
                _logger.LogError("Acker never should not be completed here");

            }
            Acker.TrySetResult(acked);
            return Task.CompletedTask;
        }

        private TaskCompletionSource<bool> Acker
        {
            set
            {
                lock (Context)
                {
                    if (Context.Items.ContainsKey(nameof(Acker)))
                    {
                        Context.Items[nameof(Acker)] = value;
                    }
                    else
                    {
                        Context.Items.TryAdd(nameof(Acker), value);
                    }
                }
            }
            get
            {
                return (TaskCompletionSource<bool>)Context.Items[(nameof(Acker))];
            }
        }
    }
}
