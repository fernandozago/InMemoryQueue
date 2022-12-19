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
        private const string RETRYCHANNEL_DESCRIPTION = "Retry Channel";
        private const string MAINCHANNEL_DESCRIPTION = "Main Channel";
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";

        private const string LOGMSG_QUEUEREADER_CANCELLED = "QueueReader For: {consumerType} Queue -- Cancelled";
        private const string LOGMSG_QUEUEREADER_FINISHED_WITH_EX = "QueueReader For: {consumerType} Queue -- Finished With Exception";
        private const string LOGMSG_QUEUEREADER_FINISHED = "QueueReader For: {consumerType} Queue -- Finished";
        private const string LOGMSG_TRACE_ITEM_ADDED_TO_RETRY_CANCELLED = "Added Item To Retry Channel from {queueName} ***** READER WAS SHUTTING DOWN ****** {item}";
        private const string LOGMSG_TRACE_ITEM_ADDED_TO_RETRY_ACK_FAILED = "Added Item To Retry Channel from {queueName} ***** FAILED TO ACK ****** {item}";
        private const string LOGMSG_DELIVER_FAIL = "Failed trying to deliver an item";
        private const string LOGMSG_REDELIVER_FAIL = "Failed trying to redeliver an item";
        private const string LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED = "Failed to to get lock... Client is disconnecting from {queueName}";
        private const string LOGMSG_READER_DISPOSED = "Reader Disposed";
        #endregion

        private readonly CancellationToken _token;
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
            _retryChannel = retryChannel;
            _channelCallBack = callBack;

            Completed = new TaskCompletionSource<bool>();
            _semaphoreSlim = new(1);

            _consumerTask = Task.WhenAll(
                ChannelConsumerCore(RETRYCHANNEL_DESCRIPTION, retryChannel.Reader),
                ChannelConsumerCore(MAINCHANNEL_DESCRIPTION, mainChannel.Reader)
            ).ContinueWith(r => Completed.TrySetResult(true), CancellationToken.None);
        }

        private async Task ChannelConsumerCore(string queueName, ChannelReader<QueueItem> reader)
        {
            try
            {
                await foreach (var item in reader.ReadAllAsync(_token).ConfigureAwait(false))
                {
                    if (_token.IsCancellationRequested || !await TryDeliverItemAsync(queueName, item).ConfigureAwait(false))
                    {
                        await AddToRetryChannel(queueName, item).ConfigureAwait(false);
                    }
                    if (_token.IsCancellationRequested)
                        break;
                }
            }
            catch (System.OperationCanceledException)
            {
                _logger.LogWarning(LOGMSG_QUEUEREADER_CANCELLED, queueName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, LOGMSG_QUEUEREADER_FINISHED_WITH_EX, queueName);
                return;
            }

            _logger.LogInformation(LOGMSG_QUEUEREADER_FINISHED, queueName);
        }

        private ValueTask AddToRetryChannel(string queueName, QueueItem item)
        {
            item.Retrying = true;
            item.RetryCount++;
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                if (_token.IsCancellationRequested)
                {
                    _logger.LogTrace(LOGMSG_TRACE_ITEM_ADDED_TO_RETRY_CANCELLED, queueName, item);
                }
                else
                {
                    _logger.LogTrace(LOGMSG_TRACE_ITEM_ADDED_TO_RETRY_ACK_FAILED, queueName, item);
                }
            }
            return _retryChannel.Writer.WriteAsync(item);
        }

        /// <summary>
        /// Deliver or Redeliver an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="item">Message to be sent</param>
        /// <returns>true (ack) | false (nack)</returns>
        private async Task<bool> TryDeliverItemAsync(string queueName, QueueItem item)
        {
            bool isRetrying = item.Retrying;
            bool isAcked = false;

            var timestamp = Stopwatch.GetTimestamp();

            if (await _semaphoreSlim.TryWaitAsync(_token).ConfigureAwait(false) is IDisposable locker)
            {
                using (locker)
                {
                    if (!_token.IsCancellationRequested)
                    {
                        try
                        {
                            isAcked = await _channelCallBack(item).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (item.Retrying)
                        {
                            _logger.LogWarning(ex, LOGMSG_REDELIVER_FAIL);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, LOGMSG_DELIVER_FAIL);
                        }
                    }
                }
            }
            else
            {
                _logger.LogTrace(LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED, queueName);
            }

            _counters.UpdateCounters(isRetrying, isAcked, timestamp);
            return isAcked;
        }

        public async ValueTask DisposeAsync()
        {
            using (_semaphoreSlim)
            {
                await _consumerTask.ConfigureAwait(false);
            }
            _logger.LogInformation(LOGMSG_READER_DISPOSED);
        }
    }

}
