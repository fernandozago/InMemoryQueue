namespace MemoryQueue.Models
{
    public sealed record QueueItem
    {
        required public string Message { get; set; }
        public bool Retrying { get; set; }
        public int RetryCount { get; set; }
    }
}
