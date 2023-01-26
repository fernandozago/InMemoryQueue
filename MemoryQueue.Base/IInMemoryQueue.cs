using MemoryQueue.Base.Models;

namespace MemoryQueue.Base;

public interface IInMemoryQueue
{
    public int ConsumersCount { get; }
    IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, CancellationToken, Task<bool>> channelCallBack, CancellationToken token);
    void RemoveReader(IInMemoryQueueReader reader);
    Task EnqueueAsync(string item, CancellationToken token = default);
    ValueTask<QueueItem?> TryPeekMainQueueAsync();
    ValueTask<QueueItem?> TryPeekRetryQueueAsync();
    QueueInfo GetInfo(bool forceUpdate = false);
    void ResetCounters();
    Task DeleteItem(QueueItem id);
}

public interface IInMemoryQueueReader : IDisposable
{
    Task Completed { get; }
}
