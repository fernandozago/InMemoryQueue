using MemoryQueue.Base.Models;
using System.Diagnostics;

namespace MemoryQueue.Base
{
    public class InMemoryQueueInfo
    {
        private readonly SemaphoreSlim _semaphore = new (1);
        private readonly InMemoryQueue _inMemoryQueue;
        private QueueInfo _queueInfo;
        private int _lastGeneratedSecond;

        public InMemoryQueueInfo(InMemoryQueue inMemoryQueue)
        {
            _inMemoryQueue = inMemoryQueue;
            _lastGeneratedSecond = new TimeSpan(Stopwatch.GetTimestamp()).Seconds;
            _queueInfo = InternalGetQueueInfo();
        }

        public QueueInfo GetQueueInfo()
        {
            var second = new TimeSpan(Stopwatch.GetTimestamp()).Seconds;
            if (_lastGeneratedSecond != second && _semaphore.Wait(0))
            {
                _lastGeneratedSecond = second;
                _queueInfo = InternalGetQueueInfo();
                _semaphore.Release();
            }
            return _queueInfo;
        }

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

                AvgAckTimeMilliseconds = _inMemoryQueue.Counters.AvgConsumptionMs
            };
            queueInfo.Consumers.AddRange(_inMemoryQueue.Consumers.Select(ParseConsumer));
            return queueInfo;
        }

        private static ConsumerInfo ParseConsumer(QueueConsumerInfo info)
        {
            var consumerInfo = new ConsumerInfo();
            consumerInfo.Counters ??= new Models.Counters();

            consumerInfo.Host = info.Host;
            consumerInfo.Id = info.Id;
            consumerInfo.Ip = info.Ip;
            consumerInfo.Name = info.Name;
            consumerInfo.Type = info.ConsumerType.ToString();

            consumerInfo.Counters.AckCounter = info.Counters?.AckCounter ?? 0;
            consumerInfo.Counters.AckPerSecond = info.Counters?.AckPerSecond ?? 0;
            consumerInfo.Counters.AvgConsumptionMs = info.Counters?.AvgConsumptionMs ?? 0;
            consumerInfo.Counters.DeliverCounter = info.Counters?.DeliverCounter ?? 0;
            consumerInfo.Counters.DeliverPerSecond = info.Counters?.DeliverPerSecond ?? 0;
            consumerInfo.Counters.NackCounter = info.Counters?.NackCounter ?? 0;
            consumerInfo.Counters.NackPerSecond = info.Counters?.NackPerSecond ?? 0;
            consumerInfo.Counters.Throttled = info.Counters?.Throttled ?? false;

            return consumerInfo;
        }
    }
}
