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

        public Task Publish(string item, string? queue) =>
            _queueManager
                .GetOrCreateQueue(queue)
                .EnqueueAsync(item);

        public ChannelReader<QueueItemReply> Consume(string clientName, string? queue, CancellationToken token)
        {
            var channel = Channel.CreateBounded<QueueItemReply>(1);
            AckTaskCompletionSource = new TaskCompletionSource<bool>();
            AckTaskCompletionSource.SetResult(true);

            _ = Task.Run(async () =>
            {
                var memoryQueue = _queueManager.GetOrCreateQueue(queue);
                var httpConnectionFeature = base.Context.Features.Get<IHttpConnectionFeature>();
                var consumerQueueInfo = new QueueConsumerInfo(QueueConsumerType.SignalR)
                {
                    Id = Context.ConnectionId,
                    Host = string.Format(ADDRESS_PORT_FORMAT, httpConnectionFeature.LocalIpAddress.ToString(), httpConnectionFeature.LocalPort),
                    Ip = string.Format(ADDRESS_PORT_FORMAT, httpConnectionFeature.RemoteIpAddress.ToString(), httpConnectionFeature.RemotePort),
                    Name = clientName ?? Context.ConnectionId
                };

                var logger = _loggerFactory.CreateLogger(string.Format(GRPC_QUEUEREADER_LOGGER_CATEGORY, memoryQueue.GetInfo().QueueName, consumerQueueInfo.ConsumerType, consumerQueueInfo.Name));

                using var tokenRegistration = token.Register(() =>
                {
                    logger.LogInformation(LOGMSG_SIGNALR_REQUEST_CANCELLED);
                    channel.Writer.Complete();
                });

                using var reader = memoryQueue.AddQueueReader(
                    consumerQueueInfo,
                    (item, callbackToken) => WriteAndAckAsync(channel.Writer, item, callbackToken),
                    token);

                try
                {
                    await channel.Reader.Completion.ConfigureAwait(false);
                    try
                    {
                        //Context may be disposed at this point.
                        AckTaskCompletionSource.TrySetCanceled();
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
            }, token);

            return channel.Reader;
        }

        private async Task<bool> WriteAndAckAsync(ChannelWriter<QueueItemReply> writer, QueueItem item, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            AckTaskCompletionSource = tcs; //Expose local reference for the Hub Context

            using var registration = token.Register(() => tcs.TrySetCanceled());
            token.ThrowIfCancellationRequested();

            if (writer.WriteAsync(new()
            {
                Message = item.Message,
                Retrying = item.Retrying,
                RetryCount = item.RetryCount
            }, token) is ValueTask write && !write.IsCompletedSuccessfully)
            {
                await write.ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return await tcs.Task.ConfigureAwait(false);
        }

        public Task Ack(bool acked)
        {
            if (AckTaskCompletionSource.Task.IsCompleted)
            {
                _logger.LogError("Acker should never be completed here");
            }
            AckTaskCompletionSource.TrySetResult(acked);
            return Task.CompletedTask;
        }

        private TaskCompletionSource<bool> AckTaskCompletionSource
        {
            set => Context.Items[nameof(AckTaskCompletionSource)] = value;
            get => (TaskCompletionSource<bool>)Context.Items[nameof(AckTaskCompletionSource)];
        }
    }
}
