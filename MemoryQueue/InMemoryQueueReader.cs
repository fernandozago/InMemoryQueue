using MemoryQueue.Counters;
using MemoryQueue.Extensions;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue
{
    internal sealed class InMemoryQueueReader : ITargetBlock<QueueItem>, IAsyncDisposable
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
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly BufferBlock<QueueItem> _queue;
        private readonly BufferBlock<QueueItem> _retryQueue;
        private readonly ActionBlock<QueueItem> _actionBlock;

        //private readonly Task _consumer;
        private readonly ILogger _logger;
        private readonly Func<QueueItem, Task<bool>> _channelCallBack;
        private readonly IDisposable _link1;
        private readonly IDisposable _link2;
        private readonly ConsumptionCounter _counters;

        internal TaskCompletionSource<bool> Completed { get; }

        public Task Completion => Completed.Task;

        public InMemoryQueueReader(string queueName, QueueConsumer consumerInfo, ConsumptionCounter counters, ILoggerFactory loggerFactory, BufferBlock<QueueItem> queue, BufferBlock<QueueItem> retryQueue, Func<QueueItem, Task<bool>> callBack, CancellationToken token)
        {
            _logger = loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName, consumerInfo.ConsumerType, consumerInfo.Name));

            _token = token;
            Completed = new TaskCompletionSource<bool>();
            _token.Register(() => Complete());

            _counters = counters;
            _channelCallBack = callBack;

            _semaphoreSlim = new(1);
            _queue = queue;
            _retryQueue = retryQueue;

            _actionBlock = new ActionBlock<QueueItem>(ConsumeMessage, new ExecutionDataflowBlockOptions()
            {
                SingleProducerConstrained = true,
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 1,
            });

            _link1 = _queue.LinkTo(_actionBlock);
            _link2 = _retryQueue.LinkTo(_actionBlock);
        }

        private async Task ConsumeMessage(QueueItem item)
        {
            if (!await TryDeliverItemAsync(item.Retrying ? "Redeliver" : "Deliver", item).ConfigureAwait(false))
            {
                item.Retrying = true;
                item.RetryCount++;
                await _retryQueue.SendAsync(item).ConfigureAwait(false);
            }
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
                _logger.LogWarning(LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED, queueName);
            }

            _counters.UpdateCounters(isRetrying, isAcked, timestamp);
            return isAcked;
        }

        public ValueTask DisposeAsync()
        {
            using (_semaphoreSlim)
            {
                _link1?.Dispose();
                _link2?.Dispose();
            }
            _logger.LogInformation(LOGMSG_READER_DISPOSED);
            return ValueTask.CompletedTask;
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, QueueItem messageValue, ISourceBlock<QueueItem>? source, bool consumeToAccept)
        {
            if (consumeToAccept || source == null)
            {
                _logger.LogWarning("ConsumeToAccept: {consumeToAccept} -- {sourceIsNull}", consumeToAccept, source is null);
                messageValue.Retrying = true;
                messageValue.RetryCount++;
                return DataflowMessageStatus.Declined;
            }

            if (_token.IsCancellationRequested)
            {
                _logger.LogWarning("Declining permanently");
                messageValue.Retrying = true;
                messageValue.RetryCount++;
                return DataflowMessageStatus.DecliningPermanently;
            }

            if (source.ReserveMessage(messageHeader, this))
            {
                if (TryDeliverItemAsync(MAINCHANNEL_DESCRIPTION, messageValue).GetAwaiter().GetResult())
                {
                    if (source.ConsumeMessage(messageHeader, this, out bool consumed) is QueueItem item && consumed)
                    {
                        return DataflowMessageStatus.Accepted;
                    }
                }

                messageValue.Retrying = true;
                messageValue.RetryCount++;
                source.ReleaseReservation(messageHeader, this);
                if (!_token.IsCancellationRequested)
                {
                    //_logger.LogWarning("Fail to deliver");
                    return DataflowMessageStatus.Postponed;
                }
                else
                {
                    _logger.LogWarning("Fail to deliver -- DecliningPermanently");
                    return DataflowMessageStatus.DecliningPermanently;
                }
            }
            else
            {
                messageValue.Retrying = true;
                messageValue.RetryCount++;
                return DataflowMessageStatus.NotAvailable;
            }
        }

        public void Complete()
        {
            Completed.TrySetResult(true);
        }

        public void Fault(Exception exception)
        {
            Completed.TrySetException(exception);
        }
    }

}
