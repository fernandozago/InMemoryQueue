using MemoryQueue.Base.Counters;

namespace MemoryQueue.Base.Models;

public sealed record QueueConsumerInfo
{
    private const string ToStringFormat = "{{ ConsumerType = {0}, Id = {1}, Name = {2}, Ip = {3}, Host = {4} }}";
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

    public override string ToString() =>
        string.Format(ToStringFormat, ConsumerType, Id, Name, Ip, Host);
}