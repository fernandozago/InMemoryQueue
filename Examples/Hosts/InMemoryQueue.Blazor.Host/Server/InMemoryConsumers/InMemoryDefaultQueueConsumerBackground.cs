using MemoryQueue;
using MemoryQueue.Models;
using MemoryQueue.Transports.InMemoryConsumer;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string queueName1 = "InMemoryQueue.Test-1";
        string queueName2 = "InMemoryQueue.Test-2";
        return Task.WhenAll(
            Publisher(queueName1, stoppingToken),
            Consumer("InMemory-Consumer-0", queueName1, true, stoppingToken),
            Consumer("InMemory-Consumer-1", queueName1, false, stoppingToken),

            Publisher(queueName2, stoppingToken),
            Consumer("InMemory-Consumer-0", queueName2, true, stoppingToken),
            Consumer("InMemory-Consumer-1", queueName2, true, stoppingToken)
        );
    }

    private async Task Publisher(string queueName, CancellationToken token)
    {
        var queue = _inMemoryQueueManager.GetOrCreateQueue(queueName);
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1);
            await queue.EnqueueAsync(DateTime.Now.ToString());
        }
    }

    private Task Consumer(string consumerName, string queueName, bool alwaysAck, CancellationToken token) =>
        _inMemoryQueueManager.CreateInMemoryConsumer(i => Task.FromResult(alwaysAck), consumerName, queueName, token);

}
