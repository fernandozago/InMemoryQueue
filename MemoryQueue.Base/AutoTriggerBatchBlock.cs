using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base
{
    public class AutoTriggerBatchBlock<T> : IPropagatorBlock<T, T[]>
    {
        private readonly BatchBlock<T> _batchBlock;
        private readonly IPropagatorBlock<T, T[]> _propagator;
        private readonly Timer _timer;
        private readonly TimeSpan _timeout;

        public AutoTriggerBatchBlock(int batchSize, TimeSpan timeout,
            GroupingDataflowBlockOptions dataflowBlockOptions)
        {
            _batchBlock = new BatchBlock<T>(batchSize, dataflowBlockOptions);
            _propagator = _batchBlock;
            _timer = new Timer(new TimerCallback(AutoTriggerCallback));
            _timeout = timeout;
        }

        private void AutoTriggerCallback(object state)
        {
            _batchBlock.TriggerBatch();
        }

        public AutoTriggerBatchBlock(int batchSize, TimeSpan timeout) : this(batchSize,
            timeout, new GroupingDataflowBlockOptions())
        {
        }

        public int BatchSize => _batchBlock.BatchSize;
        public TimeSpan Timeout => _timeout;
        public Task Completion => _batchBlock.Completion;
        public int OutputCount => _batchBlock.OutputCount;

        public void Complete() => _batchBlock.Complete();

        public void Fault(Exception exception) =>
            _propagator.Fault(exception);

        public IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions) =>
            _batchBlock.LinkTo(target, linkOptions);

        public bool TryReceive(Predicate<T[]>? filter, [MaybeNullWhen(false)] out T[] item) =>
            _batchBlock.TryReceive(filter, out item);

        public bool TryReceiveAll([NotNullWhen(true)] out IList<T[]>? items) =>
            _batchBlock.TryReceiveAll(out items);

        DataflowMessageStatus ITargetBlock<T>.OfferMessage(
            DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source,
            bool consumeToAccept)
        {
            var offerResult = _propagator.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
            if (offerResult == DataflowMessageStatus.Accepted)
                _timer.Change(_timeout, System.Threading.Timeout.InfiniteTimeSpan);
            return offerResult;
        }

        T[]? ISourceBlock<T[]>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target, out bool messageConsumed) =>
            throw new NotImplementedException(); //_receivable.ConsumeMessage(messageHeader, target, out messageConsumed);

        bool ISourceBlock<T[]>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target) =>
            throw new NotImplementedException(); //_receivable.ReserveMessage(messageHeader, target);

        void ISourceBlock<T[]>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target) =>
            throw new NotImplementedException(); //_receivable.ReleaseReservation(messageHeader, target);

    }
}
