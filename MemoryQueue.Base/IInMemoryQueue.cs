﻿using MemoryQueue.Counters;
using MemoryQueue.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoryQueue
{
    public interface IInMemoryQueue
    {
        string Name { get; }
        int MainChannelCount { get; }
        int RetryChannelCount { get; }
        int ConsumersCount { get; }
        QueueConsumptionCounter Counters { get; }
        IReadOnlyCollection<QueueConsumerInfo> Consumers { get; }

        ValueTask EnqueueAsync(string item);
        bool TryPeekMainQueue(out QueueItem? item);
        bool TryPeekRetryQueue(out QueueItem? item);
    }
}