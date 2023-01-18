namespace InMemoryQueue.Blazor.Host.Client.Models;

public sealed class QueueItem
{
    public required string Message { get; set; }
    public required bool Retrying { get; set; }
    public required int RetryCount { get; set; }
}