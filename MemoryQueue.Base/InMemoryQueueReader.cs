using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Extensions;
using MemoryQueue.Base.Models;
using MemoryQueue.Base.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MemoryQueue.Base
{
    public sealed class InMemoryQueueReader
    {
        #region Constants
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_QUEUEREADER_FINISHED_WITH_EX = "Finished With Exception";
        private const string LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED = "Failed to to get lock... Client is disconnecting";
        private const string LOGMSG_READER_DISPOSED = "Reader Disposed";
        #endregion

        private readonly ILogger _logger;
        private readonly IInMemoryQueue _refQueue;
        private readonly Task _consumerTask;
        private readonly ReaderConsumptionCounter _counters;
        private readonly ChannelReader<QueueItem> _mainReader;
        private readonly ChannelReader<QueueItem> _retryReader;
        private readonly ChannelWriter<QueueItem> _retryWriter;
        private readonly ConsumptionConsolidator _consolidator;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;

        public Task Completed => _consumerTask;

        public InMemoryQueueReader(InMemoryQueue inMemoryQueue, QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _logger = inMemoryQueue._loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, inMemoryQueue.Name, consumerInfo.ConsumerType, consumerInfo.Name));
            _refQueue = inMemoryQueue;
            _counters = new ReaderConsumptionCounter(inMemoryQueue.Counters);
            consumerInfo.Counters = _counters;
            _mainReader = inMemoryQueue._mainChannel.Reader;
            _retryReader = inMemoryQueue._retryChannel.Reader;
            _retryWriter = inMemoryQueue._retryChannel.Writer;
            _channelCallBack = callBack;

            _consolidator = new ConsumptionConsolidator(_counters.Consolidate);
            _semaphoreSlim = new SemaphoreSlim(1);
            _consumerTask = Task.WhenAll(
                ChannelReaderCore(_retryReader, token),
                ChannelReaderCore(_mainReader, token)
            ).ContinueWith(_ => _consolidator.Dispose());
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
                    await DeliverItemAsync(item, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }
                //while (!token.IsCancellationRequested && await WaitToReadAsync(token).ConfigureAwait(false))
                //{
                //    if (!token.IsCancellationRequested && TryRead(out var queueItem))
                //    {
                //        await DeliverItemAsync(queueItem, token).ConfigureAwait(false);
                //    }
                //}
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, LOGMSG_QUEUEREADER_FINISHED_WITH_EX);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, LOGMSG_QUEUEREADER_FINISHED_WITH_EX);
            }
        }

        private int _notAckedStreak = 0;
        private int NotAckedStreak
        {
            get => _notAckedStreak;
            set => _notAckedStreak = Math.Min(20, Math.Max(value, 0));
        }
        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="queueItem">Message to be sent</param>
        private async Task DeliverItemAsync(QueueItem queueItem, CancellationToken token)
        {
            var timestamp = StopwatchEx.GetTimestamp();
            bool isAcked = false;

            if (await _semaphoreSlim.TryAwaitAsync(token).ConfigureAwait(false) is IDisposable locker)
            {
                using var _ = locker;
                timestamp = StopwatchEx.GetTimestamp();
                isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);
            }

            _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
            if (!isAcked)
            {
                await _retryWriter.WriteAsync(new QueueItem(queueItem.Message, true, queueItem.RetryCount + 1)).ConfigureAwait(false);

                NotAckedStreak++;
                if (_refQueue.ConsumersCount > 1 && NotAckedStreak > 1)
                {
                    _counters.SetThrottled(true);
                    await NotAckedRateLimiter(NotAckedStreak, token).ConfigureAwait(false);
                }
            }
            else
            {
                NotAckedStreak--;
                if (NotAckedStreak == 0)
                {
                    _counters.SetThrottled(false);
                }
            }
        }

        private static async ValueTask NotAckedRateLimiter(int nackStreak, CancellationToken token)
        {
            try
            {
                await Task.Delay(nackStreak * 25, token).ConfigureAwait(false);
            }
            catch
            {
                //
            }
        }
    }

}
