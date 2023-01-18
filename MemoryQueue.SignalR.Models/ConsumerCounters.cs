namespace MemoryQueue.Transports.SignalR;

public sealed class ConsumerCounters
{
    public required long AckCounter { get; set; }
    public required long AckPerSecond { get; set; }
    public required double AvgConsumptionMs { get; set; }
    public required long DeliverCounter { get; set; }
    public required long DeliverPerSecond { get; set; }
    public required long NackCounter { get; set; }
    public required long NackPerSecond { get; set; }
    public required bool Throttled { get; set; }
}
