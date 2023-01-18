using MemoryQueue.Base.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MemoryQueue.Base;

public sealed partial class InMemoryQueueManager : IInMemoryQueueManager
{
    #region Constants

    private const string DEFAULT_QUEUE_NAME = "Default";
    private const string LOGMSG_QUEUE_CREATED = "Queue Created: '{queueName}' -- Hash: '{hash}'";
    private const string EX_INVALID_QUEUE_NAME = "Invalid Queue Name '{0}'";
    private const string LOGMSG_INVALID_QUEUE_NAME = "Failed parsing queuename {queueName}";
    private const string LOGMS_TRACE_USINGDEFAULT_QUEUENAME = "Using default queueName {defaultQueueName}";

    #endregion

    private readonly static Regex QUEUENAME_REGEX_VALIDATOR = new("^[a-z0-9-_.]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ConcurrentDictionary<int, Lazy<IInMemoryQueue>> _queues = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InMemoryQueueManager> _logger;

    public IReadOnlyCollection<Lazy<IInMemoryQueue>> ActiveQueues =>
        (IReadOnlyCollection<Lazy<IInMemoryQueue>>)_queues.Values;

    public InMemoryQueueManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<InMemoryQueueManager>();
    }

    public IInMemoryQueue GetOrCreateQueue(string? name = null)
    {
        var queueHashName = GetValidOrDefaultQueueName(name);
        return _queues.GetOrAdd(queueHashName.Item1, new Lazy<IInMemoryQueue>(() => CreateInMemoryQueue(queueHashName))).Value;
    }

    private Tuple<int, string> GetValidOrDefaultQueueName(string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogTrace(LOGMS_TRACE_USINGDEFAULT_QUEUENAME, DEFAULT_QUEUE_NAME);
            return new (QueueNameHashesGenerator.GenerateHash(DEFAULT_QUEUE_NAME), DEFAULT_QUEUE_NAME);
        }
        else if (!QUEUENAME_REGEX_VALIDATOR.IsMatch(name))
        {
            var ex = new InvalidOperationException(string.Format(EX_INVALID_QUEUE_NAME, name));
            _logger.LogError(ex, LOGMSG_INVALID_QUEUE_NAME, name);
            throw ex;
        }

        return new (QueueNameHashesGenerator.GenerateHash(name), name);
    }

    private IInMemoryQueue CreateInMemoryQueue(Tuple<int, string> queueHashName)
    {
        try
        {
            return new InMemoryQueue(queueHashName.Item2, _loggerFactory);
        }
        finally
        {
            _logger.LogInformation(LOGMSG_QUEUE_CREATED, queueHashName.Item2, queueHashName.Item1);
        }
    }
}
