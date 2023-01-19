using MemoryQueue.Base.Models;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base
{
    public sealed class InMemoryQueueBlock : ITargetBlock<QueueItem>, IDisposable
    {
        private readonly CancellationTokenRegistration _abRegistration;
        private readonly ITargetBlock<QueueItem> _actionBlock;
        private readonly Func<QueueItem, Task<bool>> _action;
        private readonly CancellationToken _token;
        private readonly ITargetBlock<QueueItem> _retryBlock;
        private readonly IDisposable _retryChannelLink;
        private readonly IDisposable _mainChannelLink;

        public InMemoryQueueBlock(Func<QueueItem, Task<bool>> action, InMemoryQueue inMemoryQueue, CancellationToken token)
        {
            _action = action;
            _token = token;
            _actionBlock = new ActionBlock<QueueItem>(ProcessItem, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });
            _abRegistration = token.Register(_actionBlock.Complete);

            _retryBlock = inMemoryQueue.RetryChannel;

            _retryChannelLink = inMemoryQueue.RetryChannel.LinkTo(this);
            _mainChannelLink = inMemoryQueue.MainChannel.LinkTo(this);
        }

        public Task Completion => 
            _actionBlock.Completion;

        public void Complete() => 
            _actionBlock.Complete();

        public void Fault(Exception exception) => 
            _actionBlock.Fault(exception);

        /// <summary>
        /// Intercepts a message before trying to offer to a ActionBlock
        /// Checks the CancellationToken, if is Cancelled, call Complete() and return DecliningPermanently 
        /// to avoid new messages to be offered to this ITargetBlock
        /// </summary>
        /// <param name="messageHeader"></param>
        /// <param name="messageValue"></param>
        /// <param name="source"></param>
        /// <param name="consumeToAccept"></param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, QueueItem messageValue, ISourceBlock<QueueItem>? source, bool consumeToAccept)
        {
            if (_token.IsCancellationRequested)
            {
                Complete();
                return DataflowMessageStatus.DecliningPermanently;
            }
            return _actionBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        /// <summary>
        /// Already received an item to process in ActionBlock
        /// Proceed trying to send the message to Consumer and after that, validate again the CancellationToken
        /// If cancelled, call Complete(), otherwise keep it active
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task ProcessItem(QueueItem item)
        {
            if (!(await _action(item).ConfigureAwait(false)))
            {
                await _retryBlock!.SendAsync(item.Retry()).ConfigureAwait(false);
            }

            if (_token.IsCancellationRequested)
            {
                Console.WriteLine("Completing ActionBlock");
                Complete();
            }
        }

        public void Dispose()
        {
            _retryChannelLink.Dispose();
            _mainChannelLink.Dispose();
            _abRegistration.Dispose();
        }
    }

}
