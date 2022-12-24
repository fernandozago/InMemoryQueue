using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MemoryQueue.Base;

public sealed class InMemoryQueue : IInMemoryQueue
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
    private readonly ConsumptionConsolidator _consolidator;
    private readonly ConcurrentDictionary<InMemoryQueueReader, QueueConsumerInfo> _readers = new();
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
        _consolidator = new ConsumptionConsolidator(Counters.Consolidate);

        _mainChannel = CreateUnboundedChannel();
        _retryChannel = CreateUnboundedChannel();
    }

    private static Channel<QueueItem> CreateUnboundedChannel() =>
        Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions()
        {
            SingleWriter = false,
            SingleReader = false
        });

    public InMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken)
    {
        var reader = new InMemoryQueueReader(this, consumerInfo, channelCallBack, cancellationToken);
        _readers.TryAdd(reader, consumerInfo);
        _logger.LogInformation(LOGMSG_READER_ADDED, consumerInfo);
        return reader;
    }

    public void RemoveReader(InMemoryQueueReader reader)
    {
        _readers.TryRemove(reader, out var value);
        _logger.LogInformation(LOGMSG_READER_REMOVED, value);
    }

    public async ValueTask EnqueueAsync(string item)
    {
        var queueItem = new QueueItem(item, false, 0);
        if (_mainChannel.Writer.WriteAsync(queueItem) is ValueTask t && !t.IsCompletedSuccessfully) 
        {
            Console.WriteLine("awaiting write");
            await t;
        }
        Counters.Publish();
        _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
    }

    public bool TryPeekMainQueue([MaybeNullWhen(false)] out QueueItem item)
    {
        if (_mainChannel.Reader.CanPeek && _mainChannel.Reader.TryPeek(out item))
        {
            return true;
        }

        item = default;
        return false;
    }

    public bool TryPeekRetryQueue([MaybeNullWhen(false)] out QueueItem item)
    {
        if (_retryChannel.Reader.CanPeek && _retryChannel.Reader.TryPeek(out item))
        {
            return true;
        }

        item = default;
        return false;
    }
}
