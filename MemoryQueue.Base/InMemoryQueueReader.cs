using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Extensions;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MemoryQueue.Base
{
    public sealed class InMemoryQueueReader : IDisposable
    {
        #region Constants
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_QUEUEREADER_FINISHED_WITH_EX = "Finished With Exception";
        private const string LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED = "Failed to to get lock... Client is disconnecting";
        private const string LOGMSG_READER_DISPOSED = "Reader Disposed";
        #endregion

        private readonly ILogger _logger;
        private readonly IInMemoryQueue _inMemoryQueue;
        private readonly Task _consumerTask;
        private readonly QueueConsumerInfo _consumerInfo;
        private readonly ReaderConsumptionCounter _counters;
        private readonly ChannelWriter<QueueItem> _retryWriter;
        private readonly ConsumptionConsolidator _consolidator;
        private readonly SemaphoreSlim _semaphoreSlim = new(1);
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;

        public Task Completed => _consumerTask;

        public InMemoryQueueReader(InMemoryQueue inMemoryQueue, QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _logger = inMemoryQueue._loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, inMemoryQueue.Name, consumerInfo.ConsumerType, consumerInfo.Name));
            _consumerInfo = consumerInfo;
            _counters = new ReaderConsumptionCounter(inMemoryQueue.Counters);
            _consumerInfo.Counters = _counters;
            _inMemoryQueue = inMemoryQueue;
            _retryWriter = inMemoryQueue._retryChannel.Writer;
            _channelCallBack = callBack;

            _consolidator = new ConsumptionConsolidator(_counters.Consolidate);
            _consumerTask = Task.WhenAll(
                ChannelReaderCore(inMemoryQueue._retryChannel.Reader, token),
                ChannelReaderCore(inMemoryQueue._mainChannel.Reader, token)
            );
        }

        /// <summary>
        /// Method responsible for reading channels [RetryChannel and MainChannel]
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ChannelReaderCore(ChannelReader<QueueItem> reader, CancellationToken token)
        {
            await Task.Yield();
            try
            {
                await foreach (var item in reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested || !await DeliverItemAsync(item, token).ConfigureAwait(false))
                    {
                        await _retryWriter.WriteAsync(new QueueItem(item.Message, true, item.RetryCount + 1)).ConfigureAwait(false);
                    }
                    token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, LOGMSG_QUEUEREADER_FINISHED_WITH_EX);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LOGMSG_QUEUEREADER_FINISHED_WITH_EX);
            }
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
        private async Task<bool> DeliverItemAsync(QueueItem queueItem, CancellationToken token)
        {
            var timestamp = StopwatchEx.GetTimestamp();
            bool isAcked = false;

            if (await _semaphoreSlim.TryAwaitAsync(token).ConfigureAwait(false) is IDisposable disposableLocker)
            {
                using var _ = disposableLocker;
                timestamp = StopwatchEx.GetTimestamp();
                try
                {
                    isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    isAcked = false;
                }
            }

            _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
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
                NotAckedStreak++;
                if (_inMemoryQueue.ConsumersCount > 1 && NotAckedStreak > 1)
                {
                    _counters.SetThrottled(true);
                    await TaskEx.SafeDelay(NotAckedStreak * 25, token).ConfigureAwait(false);
                }
            }

            return isAcked;
        }

        public void Dispose()
        {
            _semaphoreSlim.Dispose();
            _consolidator.Dispose();
        }
    }

}
