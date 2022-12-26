using MemoryQueue.Base.Counters;

namespace MemoryQueue.Base.Models;

public sealed class QueueConsumerInfo
{
    public QueueConsumerInfo(QueueConsumerType consumerType)
    {
        ConsumerType = consumerType;
    }

    public ReaderConsumptionCounter? Counters { get; set; }
    public QueueConsumerType ConsumerType { get; private set; }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public string Host { get; set; }

    public override string ToString()
    {
        return $"Name:{Name} Type:{ConsumerType} Ip:{Ip}";
    }

}
