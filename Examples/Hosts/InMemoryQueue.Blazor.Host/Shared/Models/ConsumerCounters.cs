namespace InMemoryQueue.Blazor.Host.Client.Models;

public sealed class ConsumerCounters
{
    public long AckCounter { get; set; }
    public long AckPerSecond { get; set; }
    public long NackCounter { get; set; }
    public long NackPerSecond { get; set; }
    public long DeliverCounter { get; set; }
    public long DeliverPerSecond { get; set; }
    public double AvgConsumptionMs { get; set; }

    public bool Throttled { get; set; }
}

