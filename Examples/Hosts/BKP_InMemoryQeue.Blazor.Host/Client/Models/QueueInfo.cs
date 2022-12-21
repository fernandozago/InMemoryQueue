namespace InMemoryQueue.Blazor.Host.Client.Models;

public enum QueueConsumerType
{
    GRPC = 1,
    InMemory = 2
    //WebSocket = 3,
}

public class Consumer
{
    public QueueConsumerType ConsumerType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public string Host { get; set; }

    public ConsumerCounters Counters { get; set; }
}

public class ConsumerCounters
{
    public long AckCounter { get; set; }
    public long AckPerSecond { get; set; }
    public long NackCounter { get; set; }
    public long NackPerSecond { get; set; }
    public long DeliverCounter { get; set; }
    public long DeliverPerSecond { get; set; }
    public double AvgAckTimeMilliseconds { get; set; }
}

public class Counters
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
    public double AvgAckTimeMilliseconds { get; set; }
}

public class QueueInfo
{
    public string Name { get; set; }
    public Counters Counters { get; set; }
    public long ConsumersCount { get; set; }
    public long MainChannelCount { get; set; }
    public long RetryChannelCount { get; set; }
    public List<Consumer> Consumers { get; set; }
}

public class QueueItemWrapper
{
    public QueueItem? Item { get; set; }
}

public class QueueItem
{
    public string Message { get; set; }
    public bool Retrying { get; set; }
    public int RetryCount { get; set; }
}

