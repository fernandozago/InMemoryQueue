using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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

        private readonly SemaphoreSlim _semaphoreSlim = new(1);
        private readonly ILogger _logger;
        private readonly IInMemoryQueue _inMemoryQueue;
        private readonly QueueConsumerInfo _consumerInfo;
        private readonly ReaderConsumptionCounter _counters;
        private readonly ITargetBlock<QueueItem> _retryWriter;
        private readonly ActionBlock<QueueItem> _actionBlock;
        private readonly CancellationToken _token;
        private readonly CancellationTokenRegistration _tokenRegistration;
        private readonly IDisposable _retryLink;
        private readonly IDisposable _mainLink;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;

        public Task Completed => _actionBlock.Completion;

        public InMemoryQueueReader(InMemoryQueue inMemoryQueue, QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _token = token;
            _logger = inMemoryQueue.LoggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, inMemoryQueue.Name, consumerInfo.ConsumerType, consumerInfo.Name));
            _actionBlock = new ActionBlock<QueueItem>(DeliverItemAsync, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 1
            });
            _tokenRegistration = _token.Register(() =>
            {
                _actionBlock.Complete();
            });

            _counters = new ReaderConsumptionCounter(inMemoryQueue.Counters);
            _consumerInfo = consumerInfo;
            _consumerInfo.Counters = _counters;

            _inMemoryQueue = inMemoryQueue;
            _retryWriter = inMemoryQueue.RetryChannel;
            _channelCallBack = callBack;

            //Enable Link InMemoryQueue to ActionBlock
            _retryLink = inMemoryQueue.RetryChannel.LinkTo(_actionBlock);
            _mainLink = inMemoryQueue.MainChannel.LinkTo(_actionBlock);
        }

        private int _notAckedStreak = 0;
        private int NotAckedStreak
        {
            get => _notAckedStreak;
            set => _notAckedStreak = Math.Clamp(value, 0, 30);
        }
        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="queueItem">Message to be sent</param>
        private async Task DeliverItemAsync(QueueItem queueItem)
        {
            bool isAcked = false;
            var timestamp = Stopwatch.GetTimestamp();
            try
            {
                isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, LOGMSG_ACK_FAILED_READER_CLOSING);
                _actionBlock.Complete();
                isAcked = false;
            }

            _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
            if (_token.IsCancellationRequested)
            {
                _actionBlock.Complete();
            }
            if (isAcked)
            {
                NotAckedStreak--;
                if (NotAckedStreak == 0)
                {
                    _counters.SetThrottled(false);
                }
            }
            else
            {
                await _retryWriter.SendAsync(queueItem.Retry()).ConfigureAwait(false);
                NotAckedStreak++;
                if (_inMemoryQueue.ConsumersCount > 1 && NotAckedStreak > 1)
                {
                    _counters.SetThrottled(true);
                    await TaskEx.SafeDelay(NotAckedStreak * 25, _token).ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            _tokenRegistration.Dispose();
            _retryLink.Dispose();
            _mainLink.Dispose();
            _semaphoreSlim.Dispose();
            _counters.Dispose();
        }
    }

}
