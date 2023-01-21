using MemoryQueue.Base;
using MemoryQueue.Base.Models;
using MemoryQueue.SignalR.Parsers;
using MemoryQueue.SignalR.Transports.SignalR;
using MemoryQueue.Transports.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace InMemoryQueue.Blazor.Host.Server.SignalRHub
{
    public class InMemoryQueueHubNew : Hub
    {
        private const string GRPC_QUEUEREADER_LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_SIGNALR_REQUEST_CANCELLED = "Request cancelled and client is being removed";
        private const string ADDRESS_PORT_FORMAT = "{0}:{1}";

        private readonly IInMemoryQueueManager _queueManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<InMemoryQueueHub> _logger;

        public InMemoryQueueHubNew(IInMemoryQueueManager inMemoryQueueManager, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InMemoryQueueHub>();
            _queueManager = inMemoryQueueManager;
            _loggerFactory = loggerFactory;
        }

        public Task QueueInfo(string? queue) =>
            Clients.Caller.SendAsync(nameof(QueueInfoReply), _queueManager.GetOrCreateQueue(queue).GetInfo().ToReply());

        public Task<QueueInfoReply> GetQueueInfo(string? queue)
        {
            return Task.FromResult(_queueManager.GetOrCreateQueue(queue).GetInfo().ToReply());
        }

        public Task Publish(string item, string? queue) =>
            _queueManager
                .GetOrCreateQueue(queue)
                .EnqueueAsync(item);

        public Task ConsumeNew(string? queueName)
        {
            _logger.LogInformation($"Gettin Queue {queueName}");
            var inMemoryQueue = _queueManager.GetOrCreateQueue(queueName);
            var client = Clients.Client(Context.ConnectionId);
            var token = Context.ConnectionAborted;
            var reader = inMemoryQueue.AddQueueReader(new QueueConsumerInfo(QueueConsumerType.SignalR)
            {
                Host = "teste",
                Id = Context.ConnectionId,
                Name = Context.ConnectionId,
                Ip = "teste"
            }, async (item, cts) =>
            {
                try
                {
                    return await client.InvokeAsync<bool>("ProcessQueueItem", item, cts).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    throw;
                }
            }, token);

            reader.Completed.ContinueWith(_ =>
            {
                inMemoryQueue.RemoveReader(reader);
            });

            Context.Items["inmemoryqueue"] = inMemoryQueue;
            Context.Items["reader"] = reader;
            return Task.CompletedTask;
        }
    }
}
