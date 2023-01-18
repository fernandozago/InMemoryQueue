namespace InMemoryQueue.Blazor.Host.Client.Models;

public sealed class Consumer
{
    public QueueConsumerType ConsumerType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public string Host { get; set; }

    public ConsumerCounters Counters { get; set; }
}

