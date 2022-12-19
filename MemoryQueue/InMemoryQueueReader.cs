using MemoryQueue.Counters;
using MemoryQueue.Extensions;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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

        private readonly CancellationToken _token;
        private readonly Channel<QueueItem> _mainChannel;
        private readonly Channel<QueueItem> _retryChannel;
        private readonly Task _consumerTask;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ILogger _logger;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;
        private readonly ConsumptionCounter _counters;

        internal TaskCompletionSource<bool> Completed { get; }

        public InMemoryQueueReader(string queueName, QueueConsumer consumerInfo, ConsumptionCounter counters, ILoggerFactory loggerFactory, Channel<QueueItem> mainChannel, Channel<QueueItem> retryChannel, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _counters = counters;
            _logger = loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName, consumerInfo.ConsumerType, consumerInfo.Name));
            _token = token;
            _mainChannel = mainChannel;
            _retryChannel = retryChannel;
            _channelCallBack = callBack;

            Completed = new TaskCompletionSource<bool>();
            _semaphoreSlim = new(1);

            _consumerTask = ChannelReaderCore();
        }

        private async Task ChannelReaderCore()
        {
            try
            {
                while (!_token.IsCancellationRequested && await Task.WhenAny(_mainChannel.Reader.WaitToReadAsync(_token).AsTask(), _retryChannel.Reader.WaitToReadAsync(_token).AsTask()).Unwrap())
                {
                    if (!_token.IsCancellationRequested && _retryChannel.Reader.TryRead(out var retryItem))
                    {
                        await TryDeliverItemAsync(retryItem).ConfigureAwait(false);
                        continue;
                    }

                    if (!_token.IsCancellationRequested && _mainChannel.Reader.TryRead(out var mainItem))
                    {
                        await TryDeliverItemAsync(mainItem).ConfigureAwait(false);
                        continue;
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
            Completed.TrySetResult(true);
        }

        /// <summary>
        /// Deliver or Redeliver an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="item">Message to be sent</param>
        private async Task TryDeliverItemAsync(QueueItem item)
        {
            bool isRetrying = item.Retrying;
            bool isAcked = false;

            var timestamp = Stopwatch.GetTimestamp();
            try
            {
                if (await _semaphoreSlim.TryWaitAsync(_token).ConfigureAwait(false) is IDisposable locker)
                {
                    using (locker)
                    {
                        isAcked = await _channelCallBack(item).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogWarning(LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED);
                }
            }
            finally
            {
                _counters.UpdateCounters(isRetrying, isAcked, timestamp);

                if (!isAcked)
                {
                    item.Retrying = true;
                    item.RetryCount++;
                    await _retryChannel.Writer.WriteAsync(item).ConfigureAwait(false);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            using (_semaphoreSlim)
            {
                await _consumerTask.ConfigureAwait(false);
            }
            _logger.LogTrace(LOGMSG_READER_DISPOSED);
        }
    }

}
