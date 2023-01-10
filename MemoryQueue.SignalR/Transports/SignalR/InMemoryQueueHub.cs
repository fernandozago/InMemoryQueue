using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace MemoryQueue.SignalR.Transports.SignalR
{
    public class InMemoryQueueHub : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_SIGNALR_REQUEST_CANCELLED = "Request cancelled and client is being removed";

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

        public ChannelReader<QueueItemReply> Consume(string clientName, string? queue, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateBounded<QueueItemReply>(1);

            _ = Task.Run(async () =>
            {
                Acker = new TaskCompletionSource<bool>();
                Acker.SetResult(true);

                var memoryQueue = _queueManager.GetOrCreateQueue(queue);
                var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.SignalR)
                {
                    Id = Guid.NewGuid().ToString(),
                    Host = Context.ConnectionId,
                    Ip = Context.ConnectionId,
                    Name = clientName ?? "Unknown"
                };

                var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.Name, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));

                using var channelCancelRegistration = cancellationToken.Register(() =>
                {
                    logger.LogInformation(LOGMSG_SIGNALR_REQUEST_CANCELLED);
                    channel.Writer.Complete();
                });
                
                var reader = memoryQueue.AddQueueReader(consumerQueueInfo, (item) => WriteAndAckAsync(channel.Writer, item, cancellationToken), cancellationToken);

                try
                {
                    await channel.Reader.Completion.ConfigureAwait(false);
                    try
                    {
                        //Context may be disposed at this point.
                        Acker.TrySetCanceled();
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(ex, "Error Trying to set acker to cancelled");
                        //silently ignore exception
                    }
                    await reader.Completed.ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed completing this consumer");
                }
                finally
                {
                    memoryQueue.RemoveReader(reader);
                }
            }, cancellationToken);

            return channel.Reader;
        }

        private async Task<bool> WriteAndAckAsync(ChannelWriter<QueueItemReply> writer, QueueItem item, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var registration = token.Register(() => tcs.TrySetCanceled());
            Acker = tcs;
            token.ThrowIfCancellationRequested();

            if (writer.WriteAsync(new QueueItemReply()
            {
                Message = item.Message,
                RetryCount = item.RetryCount,
                Retrying = item.Retrying
            }, token) is ValueTask write && !write.IsCompletedSuccessfully)
            {
                await write;
            }

            token.ThrowIfCancellationRequested();
            return await tcs.Task.ConfigureAwait(false);
        }

        public Task Ack(bool acked)
        {
            if (Acker.Task.IsCompleted)
            {
                _logger.LogError("Acker should never be completed here");
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
