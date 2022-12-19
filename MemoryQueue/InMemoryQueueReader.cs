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

        private readonly Channel<QueueItem> _mainChannel;
        private readonly Channel<QueueItem> _retryChannel;
        private readonly Task _consumerTask;
        private readonly ILogger _logger;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;
        private readonly ConsumptionCounter _counters;

        internal TaskCompletionSource<bool> Completed { get; }

        public InMemoryQueueReader(string queueName, QueueConsumer consumerInfo, ConsumptionCounter counters, Channel<QueueItem> mainChannel, Channel<QueueItem> retryChannel, Func<QueueItem, Task<bool>> callBack, ILoggerFactory loggerFactory, CancellationToken token)
        {
            Completed = new TaskCompletionSource<bool>();
            _logger = loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName, consumerInfo.ConsumerType, consumerInfo.Name));

            _counters = counters;
            _mainChannel = mainChannel;
            _retryChannel = retryChannel;
            _channelCallBack = callBack;

            _consumerTask = ChannelReaderCore(token);
        }

        private async Task ChannelReaderCore(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && await WaitToReadAsync(token))
                {
                    if (!token.IsCancellationRequested && TryRead(out var queueItem))
                    {
                        await TryDeliverItemAsync(queueItem).ConfigureAwait(false);
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
            finally
            {
                Completed.TrySetResult(true);
            }
        }

        /// <summary>
        /// Wait to read any of the channels [RetryChannel or MainChannel]
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task<bool> WaitToReadAsync(CancellationToken token) =>
            Task.WhenAny(_mainChannel.Reader.WaitToReadAsync(token).AsTask(), _retryChannel.Reader.WaitToReadAsync(token).AsTask()).Unwrap();

        /// <summary>
        /// Try read item from any of the channels [RetryChannel or MainChannel]
        /// Try RetryChannel First then MainChannel
        /// </summary>
        /// <param name="channelReader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool TryRead([MaybeNullWhen(false)] out QueueItem queueItem) =>
            _retryChannel.Reader.TryRead(out queueItem) || _mainChannel.Reader.TryRead(out queueItem);

        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="queueItem">Message to be sent</param>
        private async Task TryDeliverItemAsync(QueueItem queueItem)
        {
            var timestamp = Stopwatch.GetTimestamp();

            bool isRetrying = queueItem.Retrying;
            bool isAcked = await _channelCallBack(queueItem).ConfigureAwait(false);

            if (!isAcked)
            {
                queueItem.Retrying = true;
                queueItem.RetryCount++;
                await _retryChannel.Writer.WriteAsync(queueItem).ConfigureAwait(false);
            }

            _counters.UpdateCounters(isRetrying, isAcked, timestamp);
        }
    }

}
