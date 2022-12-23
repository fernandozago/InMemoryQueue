using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Models.InMemoryConsumer;

namespace GrpcService2.Services
{
    public class InMemoryDefaultQueueConsumerBackground : BackgroundService
    {
        private readonly InMemoryQueueManager _inMemoryQueueManager;
        private readonly ILogger<InMemoryDefaultQueueConsumerBackground> _logger;

        public InMemoryDefaultQueueConsumerBackground(InMemoryQueueManager inMemoryQueueManager, ILogger<InMemoryDefaultQueueConsumerBackground> logger)
        {
            _inMemoryQueueManager = inMemoryQueueManager;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            _inMemoryQueueManager.CreateInMemoryConsumer(ProcessItem, nameof(InMemoryDefaultQueueConsumerBackground), "InMemoryConsumerQueue", stoppingToken);

        private Task<bool> ProcessItem(QueueItem queueItem)
        {
            _logger.LogInformation(queueItem.Message);
            return Task.FromResult(true);
        }
            
    }
}
