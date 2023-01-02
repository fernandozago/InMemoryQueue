using MemoryQueue.Base.Counters;

namespace MemoryQueue.Base.Models;

public sealed record QueueConsumerInfo
{
    public QueueConsumerInfo(QueueConsumerType consumerType)
    {
        ConsumerType = consumerType;
    }

    public ReaderConsumptionCounter? Counters { get; set; }
    public QueueConsumerType ConsumerType { get; private set; }

    required public string Id { get; set; }
    required public string Name { get; set; }
    required public string Ip { get; set; }
    required public string Host { get; set; }

    public override string ToString()
    {
        return $"{{ ConsumerType = {ConsumerType}, Id = {Id}, Name = {Name}, Ip = {Ip}, Host = {Host} }}";
    }
}
