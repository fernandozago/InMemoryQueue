using MemoryQueue.Base.Models;
using System.Runtime.CompilerServices;

namespace MemoryQueue.Transports.SignalR
{
    public sealed class QueueItemReply
    {
        public required string Message { get; set; }
        public required bool Retrying { get; set; }
        public required int RetryCount { get; set; }

        public static implicit operator QueueItemReply(QueueItem item) =>
            new ()
            {
                Message = item.Message,
                Retrying = item.Retrying,
                RetryCount = item.RetryCount
            };
    }
}
