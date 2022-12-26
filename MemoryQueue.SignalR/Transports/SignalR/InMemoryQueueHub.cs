using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MemoryQueue.SignalR.Transports.SignalR
{
    public class InMemoryQueueHub : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_GRPC_REQUEST_CANCELLED = "Request cancelled and client is being removed";

        private readonly InMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;

        public InMemoryQueueHub(InMemoryQueueManager inMemoryQueueManager, ILoggerFactory loggerFactory)
        {
            _queueManager = inMemoryQueueManager;
            _loggerFactory = loggerFactory;
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

            InMemoryQueue memoryQueue = (InMemoryQueue)_queueManager.GetOrCreateQueue(queue);
            string id = Guid.NewGuid().ToString();
            var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.SignalR)
            {
                Id = id,
                Host = Context.ConnectionId,
                Ip = Context.ConnectionId,
                Name = clientName ?? "Unknown"
            };

            var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.Name, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));
            Channel<QueueItem> internalChannel = Channel.CreateBounded<QueueItem>(1);

            cancellationToken.Register(() =>
            {
                internalChannel.Writer.Complete();
                logger.LogInformation(LOGMSG_GRPC_REQUEST_CANCELLED);
            });

            Acker = new TaskCompletionSource<bool>();
            Acker.SetResult(true);

            var reader = memoryQueue.AddQueueReader(
                    consumerQueueInfo,
                    (item) => WriteAndAckAsync(item, internalChannel, logger, cancellationToken),
                    cancellationToken);

            await foreach (var item in internalChannel.Reader.ReadAllAsync())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return new QueueItemReply()
                {
                    Message = item.Message,
                    RetryCount = item.RetryCount,
                    Retrying = item.Retrying
                };

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            Acker.TrySetResult(false);
            await reader.Completed;
            memoryQueue.RemoveReader(reader);
            logger.LogInformation("Request finished");
        }

        private async Task<bool> WriteAndAckAsync(QueueItem item, Channel<QueueItem> internalChannel, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                Acker = new TaskCompletionSource<bool>();
                await internalChannel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                return await Acker.Task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error at WriteAndAckAsync");
            }
            return false;
        }

        public Task Ack(bool acked)
        {
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
                if (Context.Items.TryGetValue(nameof(Acker), out var obj))
                {
                    return ((TaskCompletionSource<bool>)obj);
                }
                throw new InvalidOperationException("Should not run");
            }
        }
    }
}
