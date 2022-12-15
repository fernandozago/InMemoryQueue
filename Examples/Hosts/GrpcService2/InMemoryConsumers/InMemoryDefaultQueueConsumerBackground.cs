using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Transports.InMemoryConsumer;

namespace GrpcService2.Services
{
    public class InMemoryDefaultQueueConsumerBackground : BackgroundService
    {
        private readonly InMemoryQueueManager _inMemoryQueueManager;

        public InMemoryDefaultQueueConsumerBackground(InMemoryQueueManager inMemoryQueueManager)
        {
            _inMemoryQueueManager = inMemoryQueueManager;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            _inMemoryQueueManager.CreateInMemoryConsumer(ProcessItem, nameof(InMemoryDefaultQueueConsumerBackground), "InMemoryConsumerQueue", stoppingToken);

        private Task<bool> ProcessItem(QueueItem queueItem) =>
            Task.FromResult(true);
    }
}
