using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryQueue.Base;

public interface IInMemoryQueue
{
    string Name { get; }
    int MainChannelCount { get; }
    int RetryChannelCount { get; }
    int ConsumersCount { get; }
    QueueConsumptionCounter Counters { get; }
    IReadOnlyCollection<QueueConsumerInfo> Consumers { get; }
    IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken);
    void RemoveReader(IInMemoryQueueReader reader);
    ValueTask EnqueueAsync(string item);
    ValueTask<QueueItem?> TryPeekMainQueueAsync();
    ValueTask<QueueItem?> TryPeekRetryQueueAsync();
}

public interface IInMemoryQueueReader : IDisposable
{
    Task Completed { get; }
}
