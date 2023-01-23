namespace MemoryQueue.Base.Models;

public record QueueItem
{
    public QueueItem(string message)
    {
        Message = message;
    }

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Message { get; set; }

    public bool Retrying { get; set; }

    public int RetryCount { get; set; }


    public QueueItem Retry()
    {
        return this with
        {
            Retrying = true,
            RetryCount = RetryCount + 1
        };
    }
}
