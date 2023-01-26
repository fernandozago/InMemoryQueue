using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        private readonly InMemoryQueueBlock _queueBlock;
        private readonly IInMemoryQueue _inMemoryQueue;
        private readonly ReaderConsumptionCounter _counters;
        private readonly CancellationToken _token;
        private readonly Func<QueueItem, CancellationToken, Task<bool>> _consumerCallBack;

        public Task Completed => _queueBlock.Completion;

        public InMemoryQueueReader(InMemoryQueue inMemoryQueue, QueueConsumerInfo consumerInfo, Func<QueueItem, CancellationToken, Task<bool>> callBack, CancellationToken token)
        {
            _logger = inMemoryQueue.LoggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, inMemoryQueue.Name, consumerInfo.ConsumerType, consumerInfo.Name));
            _token = token;
            _inMemoryQueue = inMemoryQueue;
            _consumerCallBack = callBack;

            _counters = consumerInfo.Counters = new ReaderConsumptionCounter(inMemoryQueue.Counters);

            _queueBlock = new InMemoryQueueBlock(DeliverItemAsync, inMemoryQueue, token);
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
        private async Task<bool> DeliverItemAsync(QueueItem queueItem)
        {
            bool isAcked = false;
            try
            {
                var timestamp = Stopwatch.GetTimestamp();
                try
                {
                    isAcked = await _consumerCallBack(queueItem, _token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, LOGMSG_ACK_FAILED_READER_CLOSING);
                    _queueBlock.Complete();
                }

                _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
                return isAcked;
            }
            finally
            {
                if (isAcked)
                {
                    await _inMemoryQueue.DeleteItem(queueItem);
                }
                await ThrottleCheck(isAcked).ConfigureAwait(false);
            }
        }

        private async ValueTask ThrottleCheck(bool isAcked)
        {
            if (isAcked)
            {
                NotAckedStreak--;
                if (NotAckedStreak == 0)
                {
                    _counters.SetThrottled(false);
                }
                return;
            }

            NotAckedStreak++;
            if (_inMemoryQueue.ConsumersCount > 1 && NotAckedStreak > 1)
            {
                _counters.SetThrottled(true);
                await TaskEx.SafeDelay(NotAckedStreak * 25, _token).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _counters.Dispose();
            _queueBlock.Dispose();
        }
    }

}
