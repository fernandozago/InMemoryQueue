using MemoryQueue.Counters;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace MemoryQueue
{
    internal sealed class InMemoryQueue : IInMemoryQueue
    {
        #region Constants

        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueue)}.{{0}}";
        private const string LOGMSG_TRACE_ITEM_QUEUED = "Item Queued {queueItem}";
        private const string LOGMSG_READER_ADDED = "Reader added for {consumerInfo}";
        private const string LOGMSG_READER_REMOVED = "Reader removed from {consumerInfo}";

        #endregion

        #region Fields
        private readonly ILogger _logger;
        internal readonly ILoggerFactory _loggerFactory;
        internal readonly Channel<QueueItem> _retryChannel;
        internal readonly Channel<QueueItem> _mainChannel;
        private readonly ConcurrentDictionary<InMemoryQueueReader, QueueConsumerInfo> _readers = new ();
        #endregion

        public string Name { get; private set; }
        public QueueConsumptionCounter Counters { get; private set; }
        public int ConsumersCount => _readers.Count;
        public int MainChannelCount => _mainChannel.Reader.Count;
        public int RetryChannelCount => _retryChannel.Reader.Count;

        public IReadOnlyCollection<QueueConsumerInfo> Consumers =>
            (IReadOnlyCollection<QueueConsumerInfo>)_readers.Values;

        public InMemoryQueue(string queueName, ILoggerFactory loggerFactory)
        {
            Name = queueName;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName));

            Counters = new();

            _mainChannel = CreateUnboundedChannel();
            _retryChannel = CreateUnboundedChannel();
        }

        private static Channel<QueueItem> CreateUnboundedChannel() =>
            Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions()
            {
                SingleWriter = false,
                SingleReader = false
            });

        internal InMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken)
        {
            var reader = new InMemoryQueueReader(this, consumerInfo, channelCallBack, cancellationToken);
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
            await _mainChannel.Writer.WriteAsync(queueItem).ConfigureAwait(false);
            Counters.Publish();
            _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
        }

        public bool TryPeekMainQueue([MaybeNullWhen(false)] out QueueItem item)
        {
            item = null;
            return _mainChannel.Reader.CanPeek && _mainChannel.Reader.TryPeek(out item);
        }

        public bool TryPeekRetryQueue([MaybeNullWhen(false)] out QueueItem item)
        {
            item = null;
            return _retryChannel.Reader.CanPeek && _retryChannel.Reader.TryPeek(out item);
        }
    }
}
