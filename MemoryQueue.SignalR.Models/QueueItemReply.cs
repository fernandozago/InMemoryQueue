namespace MemoryQueue.Transports.SignalR
{
    public sealed class QueueItemReply
    {
        public required string Message { get; set; }
        public required bool Retrying { get; set; }
        public required int RetryCount { get; set; }
    }
}
