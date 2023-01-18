using MemoryQueue.Base;
using MemoryQueue.Base.InMemoryConsumer;

namespace InMemoryQueue.Blazor.Host.Grpc.InMemoryConsumers;

public class InMemoryConsumerBackgroundService : BackgroundService
{
    private readonly IInMemoryQueueManager _inMemoryQueueManager;

    public InMemoryConsumerBackgroundService(IInMemoryQueueManager inMemoryQueueManager)
    {
        _inMemoryQueueManager = inMemoryQueueManager;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string queueName1 = "InMemoryQueue.Test-1";
        const string queueName2 = "InMemoryQueue.Test-2";
        return Task.WhenAll(
            Publisher(queueName1, stoppingToken),

            ConsumeAndRepub("InMemory-Consumer-0", queueName1, queueName2, stoppingToken),
            ConsumeAndRepub("InMemory-Consumer-1", queueName1, queueName2, stoppingToken),

            Consumer("InMemory-Consumer-0", queueName2, true, stoppingToken),
            Consumer("InMemory-Consumer-1", queueName2, false, stoppingToken),
            ConsumerMayNackSome("InMemory-Consumer-RandomNack", queueName2, stoppingToken)
        );
    }

    private Task ConsumerMayNackSome(string consumerName, string queueName, CancellationToken token) =>
        _inMemoryQueueManager.CreateInMemoryConsumer(i => Task.FromResult(Random.Shared.Next(1, 10) >= 3), consumerName, queueName, token);

    private async Task Publisher(string queueName, CancellationToken token)
    {
        await Task.Yield();
        var queue = _inMemoryQueueManager.GetOrCreateQueue(queueName);
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1, token);
            await queue.EnqueueAsync(DateTime.Now.ToString(), token);
            await queue.EnqueueAsync(DateTime.Now.ToString(), token);
        }
    }

    private Task Consumer(string consumerName, string queueName, bool alwaysAck, CancellationToken token) =>
        _inMemoryQueueManager.CreateInMemoryConsumer(i => Task.FromResult(alwaysAck), consumerName, queueName, token);

    private Task ConsumeAndRepub(string consumerName, string queueName, string repubQueueName, CancellationToken token)
    {
        var repubQueue = _inMemoryQueueManager.GetOrCreateQueue(repubQueueName);
        return _inMemoryQueueManager.CreateInMemoryConsumer(async queueItem => 
        { 
            await repubQueue.EnqueueAsync(queueItem.Message, token); 
            return true; 
        }, consumerName, queueName, token);
    }
        

}
