using MemoryQueue.Base.Models;

namespace MemoryQueue.Base;

public interface IInMemoryQueue
{
    public int ConsumersCount { get; }
    IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken token);
    void RemoveReader(IInMemoryQueueReader reader);
    ValueTask EnqueueAsync(string item, CancellationToken token = default);
    ValueTask<QueueItem?> TryPeekMainQueueAsync();
    ValueTask<QueueItem?> TryPeekRetryQueueAsync();
    QueueInfo GetInfo();
    void ResetCounters();
}

public interface IInMemoryQueueReader : IDisposable
{
    Task Completed { get; }
}
