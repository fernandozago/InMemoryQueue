namespace InMemoryQueue.Blazor.Host.Client.Models;

public sealed class Consumer
{
    public required string Type { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Ip { get; set; }
    public required string Host { get; set; }
    public required ConsumerCounters Counters { get; set; }
}

