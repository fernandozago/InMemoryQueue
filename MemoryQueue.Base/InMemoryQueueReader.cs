using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Extensions;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base
{
    public sealed class InMemoryQueueReader : IInMemoryQueueReader
    {
        #region Constants
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_QUEUEREADER_FINISHED_WITH_EX = "[{0}] Finished With Exception";
        private const string LOGMSG_ACK_FAILED_READER_CLOSING = "Failed to ack the message (Request was cancelled or client disconnected)";
        #endregion

        private readonly ILogger _logger;
        private readonly IInMemoryQueue _inMemoryQueue;
        private readonly QueueConsumerInfo _consumerInfo;
        private readonly ReaderConsumptionCounter _counters;
        private readonly BufferBlock<QueueItem> _retryWriter;
        private readonly ConsumptionConsolidator _consolidator;
        private readonly ActionBlock<QueueItem> _actionBlock;
        private readonly CancellationToken _token;
        private readonly IDisposable _retryLink;
        private readonly IDisposable _mainLink;
        private readonly SemaphoreSlim _semaphoreSlim = new(1);
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;

        public Task Completed => _actionBlock.Completion;

        public InMemoryQueueReader(InMemoryQueue inMemoryQueue, QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _logger = inMemoryQueue._loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, inMemoryQueue.Name, consumerInfo.ConsumerType, consumerInfo.Name));
            _counters = new ReaderConsumptionCounter(inMemoryQueue.Counters);
            _consumerInfo = consumerInfo;
            _consumerInfo.Counters = _counters;
            _inMemoryQueue = inMemoryQueue;
            _retryWriter = inMemoryQueue._retryChannel;
            _channelCallBack = callBack;
            _token = token;

            _consolidator = new ConsumptionConsolidator(_counters.Consolidate);
            _actionBlock = new ActionBlock<QueueItem>((item) => DeliverItemAsync(item, _token), new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });
            _token.Register(() =>
            {
                _actionBlock.Complete();
            });

            _retryLink = inMemoryQueue._retryChannel.LinkTo(_actionBlock);
            _mainLink = inMemoryQueue._mainChannel.LinkTo(_actionBlock);
        }

        private int _notAckedStreak = 0;
        private int NotAckedStreak
        {
            get => _notAckedStreak;
            set => _notAckedStreak = Math.Min(30, Math.Max(value, 0));
        }
        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="queueItem">Message to be sent</param>
        private async Task DeliverItemAsync(QueueItem queueItem, CancellationToken token)
        {
            IDisposable? disposableLocker = await _semaphoreSlim.TryAwaitAsync(token).ConfigureAwait(false);

            bool? isAcked = null;
            var timestamp = StopwatchEx.GetTimestamp();
            if (disposableLocker is not null)
            {                
                try
                {
                    isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    isAcked = null;
                }
            }

            _counters.UpdateCounters(queueItem.Retrying, isAcked == true, timestamp);
            if (isAcked == true)
            {
                NotAckedStreak--;
                if (NotAckedStreak == 0)
                {
                    _counters.SetThrottled(false);
                }
            }
            else
            {
                if (isAcked is null || token.IsCancellationRequested)
                {
                    _logger.LogWarning(LOGMSG_ACK_FAILED_READER_CLOSING);
                    _actionBlock.Complete();
                }
                else
                {
                    _logger.LogTrace("Normal -- {cancellationRequested}", token.IsCancellationRequested);
                }

                await _retryWriter.SendAsync(queueItem.Retry()).ConfigureAwait(false);
                NotAckedStreak++;
                if (_inMemoryQueue.ConsumersCount > 1 && NotAckedStreak > 1)
                {
                    _counters.SetThrottled(true);
                    await TaskEx.SafeDelay(NotAckedStreak * 25, token).ConfigureAwait(false);
                }
            }

            disposableLocker?.Dispose();
        }

        public void Dispose()
        {
            _retryLink.Dispose();
            _mainLink.Dispose();
            _semaphoreSlim.Dispose();
            _consolidator.Dispose();
        }
    }

}
