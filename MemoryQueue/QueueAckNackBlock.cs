using MemoryQueue.Models;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue
{
    internal class QueueAckNackBlock : ITargetBlock<QueueItem>
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        public Task Completion => throw new NotImplementedException();

        public void Complete()
        {
            tcs.TrySetResult();
        }

        public void Fault(Exception exception)
        {
            tcs.TrySetException(exception);
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, QueueItem messageValue, ISourceBlock<QueueItem>? source, bool consumeToAccept)
        {
            if (source is not null)
            {
                source.ConsumeMessage(messageHeader, this, out bool consumed);
            }
            return DataflowMessageStatus.NotAvailable;
        }
    }
}
