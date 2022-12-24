namespace MemoryQueue.Transports.SignalR
{
    public class QueueItemReply
    {
        public string Message { get; set; }
        public bool Retrying { get; set; }
        public int RetryCount { get; set; }
    }
}
