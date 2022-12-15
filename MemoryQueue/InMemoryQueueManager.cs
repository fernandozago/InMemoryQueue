using MemoryQueue.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MemoryQueue
{
    public sealed partial class InMemoryQueueManager
    {
        #region Constants
        
        private const string DEFAULT_QUEUE_NAME = "Default";
        private const string LOGMSG_QUEUE_CREATED = "Queue Created: '{queueName}' -- Hash: '{hash}'";
        private const string EX_INVALID_QUEUE_NAME = "Invalid Queue Name '{0}'";
        private const string LOGMSG_INVALID_QUEUE_NAME = "Failed parsing queuename {queueName}";

        #endregion

        [GeneratedRegex("^[a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex REGEX_ONLY_LETTERS();

        private readonly ConcurrentDictionary<int, InMemoryQueue> _queues = new();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<InMemoryQueueManager> _logger;
        private readonly object _locker = new ();

        public IReadOnlyCollection<IInMemoryQueue> ActiveQueues => 
            (IReadOnlyCollection<IInMemoryQueue>)_queues.Values;

        public InMemoryQueueManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<InMemoryQueueManager>();
        }

        public IInMemoryQueue GetOrCreateQueue(string? name = null)
        {
            string queueName = GetValidOrDefaultQueueName(name);
            if (_queues.TryGetValue(QueueNameHashesGenerator.GenerateHash(queueName), out var queue))
            {
                return queue;
            }
            else
            {
                lock (_locker)
                {
                    return _queues.GetOrAdd(QueueNameHashesGenerator.GenerateHash(queueName), (hash) => CreateInMemoryQueue(hash, queueName));
                }
            }
        }

        private string GetValidOrDefaultQueueName(string? name)
        {
            name = name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogTrace("Using default queueName {defaultQueueName}", DEFAULT_QUEUE_NAME);
                return DEFAULT_QUEUE_NAME;
            }
            else if (!REGEX_ONLY_LETTERS().IsMatch(name))
            {
                var ex = new InvalidOperationException(string.Format(EX_INVALID_QUEUE_NAME, name));
                _logger.LogError(ex, LOGMSG_INVALID_QUEUE_NAME, name);
                throw ex;
            }

            return name;
        }

        private InMemoryQueue CreateInMemoryQueue(int hash, string queueName)
        {
            try
            {
                return new InMemoryQueue(queueName, _loggerFactory);
            }
            finally
            {
                _logger.LogInformation(LOGMSG_QUEUE_CREATED, queueName, hash);
            }
        }
    }


}
