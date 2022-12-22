namespace InMemoryQueue.Blazor.Host.Client.Models;

public enum QueueConsumerType
{
    GRPC = 1,
    InMemory = 2
    //WebSocket = 3,
}

public record Queue
{
    public string Name { get; set; }
    public int QueueCount { get; set; }
    public int ConsumersCount { get; set; }
    public Counters Counters { get; set; }
}

public record Consumer
{
    public QueueConsumerType ConsumerType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public string Host { get; set; }

    public ConsumerCounters Counters { get; set; }
}

public record ConsumerCounters
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

public record Counters
{
    public long AckCounter { get; set; }
    public long AckPerSecond { get; set; }
    public long NackCounter { get; set; }
    public long NackPerSecond { get; set; }
    public long PubCounter { get; set; }
    public long PubPerSecond { get; set; }
    public long RedeliverCounter { get; set; }
    public long RedeliverPerSecond { get; set; }
    public long DeliverCounter { get; set; }
    public long DeliverPerSecond { get; set; }
    public double AvgConsumptionMs { get; set; }
}

public record QueueInfo
{
    public string Name { get; set; }
    public Counters Counters { get; set; }
    public long ConsumersCount { get; set; }
    public long MainChannelCount { get; set; }
    public long RetryChannelCount { get; set; }
    public List<Consumer> Consumers { get; set; }
}

public record QueueItemWrapper
{
    public QueueItem? Item { get; set; }
}

public record QueueItem
{
    public string Message { get; set; }
    public bool Retrying { get; set; }
    public int RetryCount { get; set; }
}

