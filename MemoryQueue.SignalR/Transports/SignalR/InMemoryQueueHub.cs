using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.SignalR.Parsers;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace MemoryQueue.SignalR.Transports.SignalR
{
    public class InMemoryQueueHub : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_SIGNALR_REQUEST_CANCELLED = "Request cancelled and client is being removed";
        private const string UNKNOWN_CLIENT_NAME = "Unknown";
        private const string ADDRESS_PORT_FORMAT = "{0}:{1}";

        private readonly IInMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<InMemoryQueueHub> _logger;

        public InMemoryQueueHub(IInMemoryQueueManager inMemoryQueueManager, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InMemoryQueueHub>();
            _queueManager = inMemoryQueueManager;
            _loggerFactory = loggerFactory;
        }

        public Task QueueInfo(string? queue) =>
            Clients.Caller.SendAsync(nameof(QueueInfoReply), _queueManager.GetOrCreateQueue(queue).GetInfo().ToReply());

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
                var httpConnectionFeature = base.Context.Features.Get<IHttpConnectionFeature>();
                var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.SignalR)
                {
                    Id = Guid.NewGuid().ToString(),
                    Host = string.Format(ADDRESS_PORT_FORMAT, httpConnectionFeature.LocalIpAddress.ToString(), httpConnectionFeature.LocalPort),
                    Ip = string.Format(ADDRESS_PORT_FORMAT, httpConnectionFeature.RemoteIpAddress.ToString(), httpConnectionFeature.RemotePort),
                    Name = clientName ?? UNKNOWN_CLIENT_NAME
                };

                var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.GetInfo().QueueName, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));

                using var channelCancelRegistration = cancellationToken.Register(() =>
                {
                    logger.LogInformation(LOGMSG_SIGNALR_REQUEST_CANCELLED);
                    channel.Writer.Complete();
                });

                using var reader = memoryQueue.AddQueueReader(
                    consumerQueueInfo,
                    (item) => WriteAndAckAsync(channel.Writer, item, cancellationToken),
                    cancellationToken);

                try
                {
                    await channel.Reader.Completion.ConfigureAwait(false);
                    try
                    {
                        //Context may be disposed at this point.
                        Acker.TrySetCanceled();
                    }
                    catch
                    {
                        //_logger.LogError(ex, "Error Trying to set acker to cancelled");
                        //silently ignore exception
                    }
                    await reader.Completed.ConfigureAwait(false);
                }
                catch (Exception ex)
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
