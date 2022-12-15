using MemoryQueue.Counters;
using MemoryQueue.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MemoryQueue
{
    public interface IInMemoryQueue
    {
        string Name { get; }
        int MainChannelCount { get; }
        int RetryChannelCount { get; }
        int ConsumersCount { get; }
        ConsumptionCounter Counters { get; }
        IReadOnlyCollection<QueueConsumer> Consumers { get; }

        ValueTask EnqueueAsync(string item);
        bool TryPeekMainQueue(out QueueItem? item);
        bool TryPeekRetryQueue(out QueueItem? item);
    }

    internal sealed class InMemoryQueue : IInMemoryQueue
    {
        #region Constants

        private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueue)}.{{0}}";
        private const string LOGMSG_TRACE_ITEM_QUEUED = "Item Queued {queueItem}";
        private const string LOGMSG_READER_ADDED = "Reader Added for {consumerInfo}";
        private const string LOGMSG_READER_REMOVED = "Reader Removed for {consumerInfo}";

        #endregion

        #region Fields
        private readonly Channel<QueueItem> _retryChannel;
        private readonly Channel<QueueItem> _mainChannel;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<InMemoryQueueReader, QueueConsumer> _readers = new ();
        #endregion

        public ConsumptionCounter Counters { get; private set; }
        public int ConsumersCount => _readers.Count;
        public int MainChannelCount => _mainChannel.Reader.Count;
        public int RetryChannelCount => _retryChannel.Reader.Count;
        public string Name { get; private set; }

        public IReadOnlyCollection<QueueConsumer> Consumers =>
            (IReadOnlyCollection<QueueConsumer>)_readers.Values;

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
            await _mainChannel.Writer.WriteAsync(queueItem).ConfigureAwait(false);
            Counters.Publish();
            _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
        }

        public bool TryPeekMainQueue(out QueueItem? item)
        {
            item = null;
            return _mainChannel.Reader.CanPeek && _mainChannel.Reader.TryPeek(out item);
        }

        public bool TryPeekRetryQueue(out QueueItem? item)
        {
            item = null;
            return _retryChannel.Reader.CanPeek && _retryChannel.Reader.TryPeek(out item);
        }
    }
}
