using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
    internal readonly BufferBlock<QueueItem> _retryChannel;
    internal readonly BufferBlock<QueueItem> _mainChannel;
    private readonly ConsumptionConsolidator _consolidator;
    private readonly ConcurrentDictionary<IInMemoryQueueReader, QueueConsumerInfo> _readers = new();
    #endregion

    public string Name { get; private set; }
    public QueueConsumptionCounter Counters { get; private set; }

    public int ConsumersCount => _readers.Count;
    public int MainChannelCount => _mainChannel.Count;
    public int RetryChannelCount => _retryChannel.Count;

    public IReadOnlyCollection<QueueConsumerInfo> Consumers =>
        (IReadOnlyCollection<QueueConsumerInfo>)_readers.Values;

    public InMemoryQueue(string queueName, ILoggerFactory loggerFactory)
    {
        Name = queueName;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName));

        Counters = new();
        _consolidator = new ConsumptionConsolidator(Counters.Consolidate);

        _mainChannel = new();
        _retryChannel = new();
    }

    public IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken cancellationToken)
    {
        var reader = new InMemoryQueueReader(this, consumerInfo, channelCallBack, cancellationToken);
        _readers.TryAdd(reader, consumerInfo);
        _logger.LogInformation(LOGMSG_READER_ADDED, consumerInfo);
        return reader;
    }

    public void RemoveReader(IInMemoryQueueReader reader)
    {
        using (reader)
        {
            _readers.TryRemove(reader, out var value);
            _logger.LogInformation(LOGMSG_READER_REMOVED, value);
        }
    }

    public async ValueTask EnqueueAsync(string item)
    {
        var queueItem = new QueueItem(item, false, 0);
        if (await _mainChannel.SendAsync(queueItem))
        {
            Counters.Publish();
            _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
        }
    }

    public bool TryPeekMainQueue([MaybeNullWhen(false)] out QueueItem item)
    {
        item = default;
        return false;
    }

    public bool TryPeekRetryQueue([MaybeNullWhen(false)] out QueueItem item)
    {
        item = default;
        return false;
    }
}
