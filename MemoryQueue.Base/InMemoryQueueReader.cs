using MemoryQueue.Base.Counters;
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
            _consumerTask = ChannelReaderCore(token).ContinueWith(_ => _consolidator.Dispose());
        }

        /// <summary>
        /// Method responsible for reading channels [RetryChannel and MainChannel]
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ChannelReaderCore(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && await WaitToReadAsync(token).ConfigureAwait(false))
                {
                    if (!token.IsCancellationRequested && TryRead(out var queueItem))
                    {
                        await DeliverItemAsync(queueItem, token).ConfigureAwait(false);
                    }
                }
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

        /// <summary>
        /// Wait to read any of the channels [RetryChannel or MainChannel]
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task<bool> WaitToReadAsync(CancellationToken token) =>
            Task.WhenAny(_mainReader.WaitToReadAsync(token).AsTask(), _retryReader.WaitToReadAsync(token).AsTask()).Unwrap();

        /// <summary>
        /// Try read item from any of the channels [RetryChannel or MainChannel]
        /// Try RetryChannel First then MainChannel
        /// </summary>
        /// <param name="channelReader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool TryRead([MaybeNullWhen(false)] out QueueItem queueItem) =>
            _retryReader.TryRead(out queueItem) || _mainReader.TryRead(out queueItem);


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

            bool isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);

            _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
            if (!isAcked)
            {
                queueItem.Retrying = true;
                queueItem.RetryCount++;
                await _retryWriter.WriteAsync(queueItem).ConfigureAwait(false);

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
