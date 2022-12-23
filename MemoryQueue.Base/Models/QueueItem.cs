namespace MemoryQueue.Base.Models;

public sealed class QueueItem
{
    public string Message { get; set; }
    public bool Retrying { get; set; }
    public int RetryCount { get; set; }
}
