using Dapper.Contrib.Extensions;
using System.Diagnostics;

namespace MemoryQueue.Base.Models;

public sealed record QueueItem
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
            RetryCount = RetryCount + 1,
            Retrying = true
        };
    }
}
