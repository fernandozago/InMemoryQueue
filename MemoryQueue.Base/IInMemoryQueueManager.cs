namespace MemoryQueue.Base
{
    public interface IInMemoryQueueManager
    {

        public IReadOnlyCollection<Lazy<IInMemoryQueue>> ActiveQueues { get; }

        public IInMemoryQueue GetOrCreateQueue(string? name = null);

    }
}