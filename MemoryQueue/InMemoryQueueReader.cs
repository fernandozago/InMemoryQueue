using MemoryQueue.Counters;
using MemoryQueue.Extensions;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue
{
    internal sealed class InMemoryQueueReader : IAsyncDisposable
    {
        #region Constants
        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueueReader)}.{{0}}.{{1}}-[{{2}}]";
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

            _actionBlock = new ActionBlock<QueueItem>(ConsumeMessage, new ExecutionDataflowBlockOptions()
            {
                SingleProducerConstrained = true,
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 1,
            });

            _token.Register(() =>
            {
                _actionBlock.Complete();
                Completed.TrySetResult(true);
            });

            _counters = counters;
            _channelCallBack = callBack;

            _semaphoreSlim = new(1);
            _queue = queue;
            _retryQueue = retryQueue;

            _link2 = _retryQueue.LinkTo(_actionBlock);
            _link1 = _queue.LinkTo(_actionBlock);
        }

        private Task ConsumeMessage(QueueItem item) =>
            TryDeliverItemAsync(item.Retrying ? "Redeliver" : "Deliver", item.Retrying, item);

        /// <summary>
        /// Deliver or Redeliver an message to some consumer and awaits for the ACK(true)/NACK(false) result
        /// </summary>
        /// <param name="item">Message to be sent</param>
        /// <returns>true (ack) | false (nack)</returns>
        private async Task<bool> TryDeliverItemAsync(string queueName, bool isRetrying, QueueItem item)
        {
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
                    _logger.LogWarning(LOGMSG_TRACE_FAILED_GETTING_LOCK_CLIENT_DISCONNECTING_OR_DISCONNECTED, queueName);
                }

                return isAcked;
            }
            finally
            {
                _counters.UpdateCounters(isRetrying, isAcked, timestamp);

                if (!isAcked)
                {
                    item.Retrying = true;
                    item.RetryCount++;
                    if (!await _retryQueue.SendAsync(item).ConfigureAwait(false))
                    {
                        _logger.LogCritical("Possible Loose of message {item}", item);
                    }
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            _semaphoreSlim.Dispose();
            _link1.Dispose();
            _link2.Dispose();
            _logger.LogInformation(LOGMSG_READER_DISPOSED);
            return ValueTask.CompletedTask;
        }
    }

}
