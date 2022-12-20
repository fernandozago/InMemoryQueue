using MemoryQueue.Counters;

namespace MemoryQueue.Models
{
    public sealed record QueueConsumerInfo
    {
        public QueueConsumerInfo(QueueConsumerType consumerType)
        {
            ConsumerType = consumerType;
        }

        public ReaderConsumptionCounter? Counters { get; internal set; }
        public QueueConsumerType ConsumerType { get; private set; }

        required public string Id { get; set; }
        required public string Name { get; set; }
        required public string Ip { get; set; }
        required public string Host { get; set; }
        
    }
}
