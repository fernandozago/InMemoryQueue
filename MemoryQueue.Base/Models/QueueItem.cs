namespace MemoryQueue.Base.Models;

public struct QueueItem
{
    public QueueItem(string message, bool retrying, int retryCount)
    {
        Message = message;
        Retrying = retrying;
        RetryCount = retryCount;
    }

    public string Message { get; private set; }
    public bool Retrying { get; private set; }
    public int RetryCount { get; private set; }

    public QueueItem Retry()
    {
        Retrying = true;
        RetryCount++;
        return this;
    }
}
