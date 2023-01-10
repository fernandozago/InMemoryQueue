namespace MemoryQueue.Base.Models;

public record QueueItem
{
    public QueueItem(string message)
    {
        Message = message;
    }

    public string Message { get; private set; }
    public bool Retrying { get; private set; }
    public int RetryCount { get; private set; }

    public QueueItem Retry()
    {
        return this with
        {
            Retrying = true,
            RetryCount = RetryCount + 1
        };
    }
}
