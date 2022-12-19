using MemoryQueue.Counters;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue
{
    internal sealed class InMemoryQueue : IInMemoryQueue
    {
        #region Constants

        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueue)}.{{0}}";
        private const string LOGMSG_TRACE_ITEM_QUEUED = "Item Queued {queueItem}";
        private const string LOGMSG_READER_ADDED = "Reader Added for {consumerInfo}";
        private const string LOGMSG_READER_REMOVED = "Reader Removed for {consumerInfo}";

        #endregion

        #region Fields
        private readonly BufferBlock<QueueItem> _mainChannel;
        private readonly BufferBlock<QueueItem> _retryChannel;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<InMemoryQueueReader, QueueConsumer> _readers = new ();
        #endregion

        public ConsumptionCounter Counters { get; private set; }
        public int ConsumersCount => _readers.Count;
        public int MainChannelCount => _mainChannel.Count;
        public int RetryChannelCount => _retryChannel.Count;
        public string Name { get; private set; }

        public IReadOnlyCollection<QueueConsumer> Consumers =>
            (IReadOnlyCollection<QueueConsumer>)_readers.Values;

        public InMemoryQueue(string queueName, ILoggerFactory loggerFactory)
        {
            Name = queueName;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName));

            Counters = new();

            _mainChannel = new BufferBlock<QueueItem>();
            _retryChannel = new BufferBlock<QueueItem>();
        }

        internal InMemoryQueueReader AddQueueReader(QueueConsumer consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken)
        {
            var reader = new InMemoryQueueReader(Name, consumerInfo, Counters, _loggerFactory, _mainChannel, _retryChannel, channelCallBack, cancellationToken);
            _readers.TryAdd(reader, consumerInfo);
            _logger.LogInformation(LOGMSG_READER_ADDED, consumerInfo);
            return reader;
        }

        internal void RemoveReader(InMemoryQueueReader reader)
        {
            _readers.TryRemove(reader, out var value);
            _logger.LogInformation(LOGMSG_READER_REMOVED, value);
        }

        public async ValueTask EnqueueAsync(string item)
        {
            var queueItem = new QueueItem() { Message = item };
            await _mainChannel.SendAsync(queueItem).ConfigureAwait(false);
            Counters.Publish();
            _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
        }

        public bool TryPeekMainQueue(out QueueItem? item)
        {
            item = null;
            return false;
        }

        public bool TryPeekRetryQueue(out QueueItem? item)
        {
            item = null;
            return false;
        }
    }
}
