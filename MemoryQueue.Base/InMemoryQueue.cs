using MemoryQueue.Base.Counters;
using MemoryQueue.Base.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace MemoryQueue.Base;

public sealed class InMemoryQueue : IInMemoryQueue
{

    #region Constants

    private const string LOGGER_CATEGORY = $"{nameof(InMemoryQueue)}.{{0}}";
    private const string LOGMSG_TRACE_ITEM_QUEUED = "Item Queued {queueItem}";
    private const string LOGMSG_READER_ADDED = "Reader added for consumer {consumerInfo}";
    private const string LOGMSG_READER_REMOVED = "Reader removed for consumer {consumerInfo}";

    #endregion

    #region Fields
    private readonly ILogger _logger;
    private readonly InMemoryQueueInfo _inMemoryQueueInfoService;
    private readonly ConcurrentDictionary<IInMemoryQueueReader, QueueConsumerInfo> _readers = new();
    #endregion

    #region Internal Properties
    internal ILoggerFactory LoggerFactory { get; private set; }
    internal BufferBlock<QueueItem> RetryChannel { get; private set; }
    internal BufferBlock<QueueItem> MainChannel { get; private set; }
    internal QueueConsumptionCounter Counters { get; private set; }
    internal string Name { get; private set; }
    internal int MainChannelCount => MainChannel.Count;
    internal int RetryChannelCount => RetryChannel.Count;
    internal IReadOnlyCollection<QueueConsumerInfo> Consumers =>
        (IReadOnlyCollection<QueueConsumerInfo>)_readers.Values;
    #endregion

    #region Public Properties
    public int ConsumersCount => _readers.Count; 
    #endregion

    public InMemoryQueue(string queueName, ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        _logger = LoggerFactory.CreateLogger(string.Format(LOGGER_CATEGORY, queueName));
        Name = queueName;

        Counters = new();
        MainChannel = new();
        RetryChannel = new();

        _inMemoryQueueInfoService = new InMemoryQueueInfo(this);
    }

    public IInMemoryQueueReader AddQueueReader(QueueConsumerInfo consumerInfo, Func<QueueItem, Task<bool>> channelCallBack, CancellationToken token)
    {
        var reader = new InMemoryQueueReader(this, consumerInfo, channelCallBack, token);
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

    public async Task EnqueueAsync(string item, CancellationToken token = default)
    {
        var queueItem = new QueueItem(item);
        if (await MainChannel.SendAsync(queueItem, token))
        {
            Counters.Publish();
            _logger.LogTrace(LOGMSG_TRACE_ITEM_QUEUED, queueItem);
        }
    }

    public async ValueTask<QueueItem?> TryPeekMainQueueAsync()
    {
        var ts = Stopwatch.GetTimestamp();
        if (MainChannel.TryReceive(out var item))
        {
            await AddToRetryQueueAsync(item.Retrying, item, ts).ConfigureAwait(false);
            return item;
        }
        return null;
    }

    public async ValueTask<QueueItem?> TryPeekRetryQueueAsync()
    {
        var ts = Stopwatch.GetTimestamp();
        if (RetryChannel.TryReceive(out var item))
        {
            await AddToRetryQueueAsync(item.Retrying, item, ts).ConfigureAwait(false);
            return item;
        }
        return null;
    }

    public void ResetCounters() =>
        Counters.ResetCounters();

    public QueueInfo GetInfo() =>
        _inMemoryQueueInfoService.GetQueueInfo();

    private async Task AddToRetryQueueAsync(bool isRetrying, QueueItem item, long ts)
    {
        await RetryChannel.SendAsync(item.Retry());
        Counters.UpdateCounters(isRetrying, false, ts);
    }
}
