using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MemoryQueue.SignalR.Transports.SignalR
{
    public class MyHub : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_GRPC_REQUEST_CANCELLED = "Request cancelled and client is being removed";

        private readonly InMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;

        public MyHub(InMemoryQueueManager inMemoryQueueManager, ILoggerFactory loggerFactory)
        {
            _queueManager = inMemoryQueueManager;
            _loggerFactory = loggerFactory;
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
