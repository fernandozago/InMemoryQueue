using MemoryQueue.Counters;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MemoryQueue
{
    internal sealed class InMemoryQueueReader : IAsyncDisposable
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

        public InMemoryQueueReader(string queueName, QueueConsumer consumerInfo, ConsumptionCounter counters, ILoggerFactory loggerFactory, Channel<QueueItem> mainChannel, Channel<QueueItem> retryChannel, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
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
                while (!token.IsCancellationRequested && await Task.WhenAny(_mainChannel.Reader.WaitToReadAsync(token).AsTask(), _retryChannel.Reader.WaitToReadAsync(token).AsTask()).Unwrap())
                {
                    if (TryReadItem(out var item, token))
                    {
                        await TryDeliverItemAsync(item).ConfigureAwait(false);
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
        /// Try read a pending item
        /// </summary>
        /// <param name="channelReader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool TryReadItem([MaybeNullWhen(false)] out QueueItem item, CancellationToken token)
        {
            item = null;
            if (!token.IsCancellationRequested && _retryChannel.Reader.TryRead(out item))
            {
                return true;
            }

            return !token.IsCancellationRequested && _mainChannel.Reader.TryRead(out item);
        }

        /// <summary>
        /// Publish an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="item">Message to be sent</param>
        private async Task TryDeliverItemAsync(QueueItem item)
        {
            var timestamp = Stopwatch.GetTimestamp();

            bool isRetrying = item.Retrying;
            bool isAcked = await _channelCallBack(item).ConfigureAwait(false);

            if (!isAcked)
            {
                item.Retrying = true;
                item.RetryCount++;
                await _retryChannel.Writer.WriteAsync(item).ConfigureAwait(false);
            }

            _counters.UpdateCounters(isRetrying, isAcked, timestamp);
        }

        public async ValueTask DisposeAsync()
        {
            await _consumerTask.ConfigureAwait(false);
            _logger.LogTrace(LOGMSG_READER_DISPOSED);
        }
    }

}
