using MemoryQueue.Base.Models;
using System.Diagnostics;

namespace MemoryQueue.Base
{
    public class InMemoryQueueInfo
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly InMemoryQueue _inMemoryQueue;
        private QueueInfo _queueInfo;
        private int _lastGeneratedSecond;

        public InMemoryQueueInfo(InMemoryQueue inMemoryQueue)
        {
            _inMemoryQueue = inMemoryQueue;
            _lastGeneratedSecond = GetCurrentSecond();
            _queueInfo = InternalGetQueueInfo();
        }

        public QueueInfo GetQueueInfo()
        {
            var second = GetCurrentSecond();
            if (_lastGeneratedSecond != second && _semaphore.Wait(0))
            {
                _lastGeneratedSecond = second;
                _queueInfo = InternalGetQueueInfo();
                _semaphore.Release();
            }
            return _queueInfo;
        }

        private int GetCurrentSecond() =>
            new TimeSpan(Stopwatch.GetTimestamp()).Seconds;

        private QueueInfo InternalGetQueueInfo()
        {
            var collectDate = DateTime.Now;
            int mainQueueSize = _inMemoryQueue.MainChannelCount;
            int retryQueueSize = _inMemoryQueue.RetryChannelCount;
            var queueInfo = new QueueInfo()
            {
                CollectDate = collectDate,
                QueueName = _inMemoryQueue.Name,
                QueueSize = mainQueueSize + retryQueueSize,
                MainQueueSize = mainQueueSize,
                RetryQueueSize = retryQueueSize,

                ConcurrentConsumers = _inMemoryQueue.ConsumersCount,

                AckCounter = _inMemoryQueue.Counters.AckCounter,
                AckPerSecond = _inMemoryQueue.Counters.AckPerSecond,

                NackCounter = _inMemoryQueue.Counters.NackCounter,
                NackPerSecond = _inMemoryQueue.Counters.NackPerSecond,

                PubCounter = _inMemoryQueue.Counters.PubCounter,
                PubPerSecond = _inMemoryQueue.Counters.PubPerSecond,

                RedeliverCounter = _inMemoryQueue.Counters.RedeliverCounter,
                RedeliverPerSecond = _inMemoryQueue.Counters.RedeliverPerSecond,

                DeliverCounter = _inMemoryQueue.Counters.DeliverCounter,
                DeliverPerSecond = _inMemoryQueue.Counters.DeliverPerSecond,

                AvgConsumptionMs = _inMemoryQueue.Counters.AvgConsumptionMs,

                Consumers = ParseConsumers(_inMemoryQueue.Consumers).ToList()
            };
            return queueInfo;
        }

        private static IEnumerable<ConsumerInfo> ParseConsumers(IReadOnlyCollection<QueueConsumerInfo> consumers) =>
            consumers.Select(info => new ConsumerInfo()
            {
                Host = info.Host,
                Id = info.Id,
                Ip = info.Ip,
                Name = info.Name,
                Type = info.ConsumerType.ToString(),
                Counters = new()
                {
                    AckCounter = info.Counters?.AckCounter ?? 0,
                    AckPerSecond = info.Counters?.AckPerSecond ?? 0,
                    AvgConsumptionMs = info.Counters?.AvgConsumptionMs ?? 0,
                    DeliverCounter = info.Counters?.DeliverCounter ?? 0,
                    DeliverPerSecond = info.Counters?.DeliverPerSecond ?? 0,
                    NackCounter = info.Counters?.NackCounter ?? 0,
                    NackPerSecond = info.Counters?.NackPerSecond ?? 0,
                    Throttled = info.Counters?.Throttled ?? false
                }
            });
    }
}
