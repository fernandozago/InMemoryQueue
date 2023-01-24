using MemoryQueue.Base.Models;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base
{
    public sealed class InMemoryQueueBlock : ITargetBlock<QueueItem>, IDisposable
    {
        private readonly CancellationTokenRegistration _tokenRegistration;
        private readonly ITargetBlock<QueueItem> _actionBlock;
        private readonly Func<QueueItem, Task<bool>> _callback;
        private readonly CancellationToken _token;
        private readonly ITargetBlock<QueueItem> _retryBlock;
        private readonly IDisposable _retryChannelLink;
        private readonly IDisposable _mainChannelLink;

        public InMemoryQueueBlock(Func<QueueItem, Task<bool>> action, InMemoryQueue inMemoryQueue, CancellationToken token)
        {
            _callback = action;
            _token = token;
            _retryBlock = inMemoryQueue.RetryQueue;

            _actionBlock = CreateConfiguredTargetBlock();
            _tokenRegistration = _token.Register(_actionBlock.Complete);
            _retryChannelLink = inMemoryQueue.RetryQueue.LinkTo(this);
            _mainChannelLink = inMemoryQueue.MainQueue.LinkTo(this);
        }

        private ITargetBlock<QueueItem> CreateConfiguredTargetBlock() =>
            new ActionBlock<QueueItem>(ProcessItemAsync, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });

        public Task Completion => 
            _actionBlock.Completion;

        public void Complete() => 
            _actionBlock.Complete();

        public void Fault(Exception exception) =>
            _actionBlock.Fault(exception);

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, QueueItem messageValue, ISourceBlock<QueueItem>? source, bool consumeToAccept)
        {
            if (CompleteIfCancelled())
            {
                return DataflowMessageStatus.DecliningPermanently;
            }
            return _actionBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CompleteIfCancelled()
        {
            if (!_token.IsCancellationRequested)
            {
                return false;
            }

            Complete();
            return true;
        }

        private async Task ProcessItemAsync(QueueItem item)
        {
            var ack = await _callback(item).ConfigureAwait(false);
            CompleteIfCancelled();
            if (!ack)
            {
                await _retryBlock.SendAsync(item.Retry()).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _retryChannelLink.Dispose();
            _mainChannelLink.Dispose();
            _tokenRegistration.Dispose();
        }
    }

}
