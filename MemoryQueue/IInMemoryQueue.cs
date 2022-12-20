using MemoryQueue.Counters;
using MemoryQueue.Models;

namespace MemoryQueue
{
    public interface IInMemoryQueue
    {
        string Name { get; }
        int MainChannelCount { get; }
        int RetryChannelCount { get; }
        int ConsumersCount { get; }
        ConsumptionCounter Counters { get; }
        IReadOnlyCollection<QueueConsumerInfo> Consumers { get; }

        ValueTask EnqueueAsync(string item);
        bool TryPeekMainQueue(out QueueItem? item);
        bool TryPeekRetryQueue(out QueueItem? item);
    }
}
