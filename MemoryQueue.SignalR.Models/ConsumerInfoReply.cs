namespace MemoryQueue.Transports.SignalR;

public sealed class ConsumerInfoReply
{
    public required ConsumerCounters Counters { get; set; }
    public required string Host { get; set; }
    public required string Id { get; set; }
    public required string Ip { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}
