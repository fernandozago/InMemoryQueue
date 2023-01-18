using MemoryQueue.Base.Models;

namespace MemoryQueue.Base.InMemoryConsumer;

public static class InMemoryQueueExtensions
{
    private const string IN_MEMORY_CONSUMER_NA = "N/A";

    public static async Task CreateInMemoryConsumer(this IInMemoryQueue inMemoryQueue,
        Func<QueueItem, CancellationToken, Task<bool>> callBack, string? consumerName = null, CancellationToken token = default)
    {
        var id = Guid.NewGuid().ToString();
        using var reader = inMemoryQueue.AddQueueReader(new QueueConsumerInfo(QueueConsumerType.InMemory)
        {
            Id = id,
            Name = consumerName ?? id,
            Host = IN_MEMORY_CONSUMER_NA,
            Ip = IN_MEMORY_CONSUMER_NA,
        }, callBack, token);

        await reader.Completed.ConfigureAwait(false);
        inMemoryQueue.RemoveReader(reader);
    }

    public static Task CreateInMemoryConsumer(this IInMemoryQueueManager inMemoryQueueManager,
        Func<QueueItem, CancellationToken, Task<bool>> callBack, string? consumerName = null, string? queueName = null, CancellationToken token = default) =>
            inMemoryQueueManager.GetOrCreateQueue(queueName).CreateInMemoryConsumer(callBack, consumerName, token);
}
