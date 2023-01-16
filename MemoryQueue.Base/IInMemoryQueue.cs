using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;

namespace MemoryQueue.Base;

public interface IInMemoryQueue
{
    public int ConsumersCount { get; }
    QueueConsumptionCounter Counters { get; }
    IReadOnlyCollection<QueueConsumerInfo> Consumers { get; }
    IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken);
    void RemoveReader(IInMemoryQueueReader reader);
    ValueTask EnqueueAsync(string item);
    ValueTask<QueueItem?> TryPeekMainQueueAsync();
    ValueTask<QueueItem?> TryPeekRetryQueueAsync();
    QueueInfo GetInfo();
}

public interface IInMemoryQueueReader : IDisposable
{
    Task Completed { get; }
}
