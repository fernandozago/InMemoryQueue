using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Transports.InMemoryConsumer;

namespace InMemoryQueue.Blazor.Host.Grpc.InMemoryConsumers;

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
        Task.WhenAll(
            _inMemoryQueueManager.CreateInMemoryConsumer(ProcessItem, "InMemory-Consumer-1", "Default", stoppingToken),
            _inMemoryQueueManager.CreateInMemoryConsumer(ProcessItem, "InMemory-Consumer-2", "Default", stoppingToken)
        );

    private Task<bool> ProcessItem(QueueItem queueItem)
    {
        return Task.FromResult(true);
    }

}
