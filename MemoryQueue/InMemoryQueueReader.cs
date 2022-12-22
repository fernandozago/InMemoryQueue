using Grpc.Core;
using MemoryQueue.Counters;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace MemoryQueue
{
    internal sealed class InMemoryQueueReader
    {
        #region Constants
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
        private const string LOGMSG_QUEUEREADER_FINISHED_WITH_EX = "Finished With Exception";
        private const string LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED = "Failed to to get lock... Client is disconnecting";
        private const string LOGMSG_READER_DISPOSED = "Reader Disposed";
        #endregion

        private readonly ILogger _logger;
        private readonly Task _consumerTask;
        private readonly ReaderConsumptionCounter _counters;
        private readonly ChannelReader<QueueItem> _mainReader;
        private readonly ChannelReader<QueueItem> _retryReader;
        private readonly ChannelWriter<QueueItem> _retryWriter;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;

        internal Task Completed => _consumerTask;

        public InMemoryQueueReader(string queueName, QueueConsumerInfo consumerInfo, QueueConsumptionCounter counters, Channel<QueueItem> mainChannel, Channel<QueueItem> retryChannel, Func<QueueItem, Task<bool>> callBack, ILoggerFactory loggerFactory, CancellationToken token)
        {
            _logger = loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName, consumerInfo.ConsumerType, consumerInfo.Name));
            _counters = new ReaderConsumptionCounter(counters);
            consumerInfo.Counters = _counters;
            _mainReader = mainChannel.Reader;
            _retryReader = retryChannel.Reader;
            _retryWriter = retryChannel.Writer;
            _channelCallBack = callBack;

            _consumerTask = ChannelReaderCore(token).ContinueWith(_ => _counters.Dispose());
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


        private long _notAckedStreak = 0;
        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="queueItem">Message to be sent</param>
        private async Task DeliverItemAsync(QueueItem queueItem, CancellationToken token)
        {
            var timestamp = Stopwatch.GetTimestamp();

            bool isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);

            _counters.UpdateCounters(queueItem.Retrying, isAcked, timestamp);
            if (!isAcked)
            {
                queueItem.Retrying = true;
                queueItem.RetryCount++;
                await _retryWriter.WriteAsync(queueItem).ConfigureAwait(false);
                await NotAckedRateLimiter(token).ConfigureAwait(false);
            }
            else
            {
                _notAckedStreak = 0;
                _counters.SetThrottled(false);
            }
        }

        private async ValueTask NotAckedRateLimiter(CancellationToken token)
        {           
            //First NACK -- Dont wait
            if (_notAckedStreak == 0)
            {
                _notAckedStreak++;
                return;
            }

            int waitFor = 500;
            if (_notAckedStreak > 0 && _notAckedStreak < 10)
            {
                waitFor = (int)_notAckedStreak * 50;
                _notAckedStreak++;
            }

            _counters.SetThrottled(true);
            try
            {
                await Task.Delay(waitFor, token).ConfigureAwait(false);
            }
            catch
            {
                //
            }
        }
    }

}
