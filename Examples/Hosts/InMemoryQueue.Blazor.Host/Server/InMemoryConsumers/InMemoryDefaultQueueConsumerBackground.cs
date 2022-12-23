using MemoryQueue;
using MemoryQueue.Models.InMemoryConsumer;

namespace InMemoryQueue.Blazor.Host.Grpc.InMemoryConsumers;

public class InMemoryConsumerBackgroundService : BackgroundService
{
    private readonly InMemoryQueueManager _inMemoryQueueManager;
    private readonly ILogger<InMemoryConsumerBackgroundService> _logger;

    public InMemoryConsumerBackgroundService(InMemoryQueueManager inMemoryQueueManager, ILogger<InMemoryConsumerBackgroundService> logger)
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
            ConsumerMayNackSome("InMemory-Consumer-RandomNack", queueName1, stoppingToken),

            Publisher(queueName2, stoppingToken),
            Consumer("InMemory-Consumer-0", queueName2, true, stoppingToken),
            Consumer("InMemory-Consumer-1", queueName2, true, stoppingToken)
        );
    }

    private Task ConsumerMayNackSome(string consumerName, string queueName, CancellationToken token) =>
        _inMemoryQueueManager.CreateInMemoryConsumer(i => Task.FromResult(Random.Shared.Next(1, 10) >= 3), consumerName, queueName, token);

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
