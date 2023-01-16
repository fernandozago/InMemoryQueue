using MemoryQueue.Base.Models;
using System.Diagnostics;

namespace MemoryQueue.Base
{
    public class InMemoryQueueInfo
    {
        private readonly SemaphoreSlim _semaphore = new (1);
        private readonly InMemoryQueue _inMemoryQueue;
        private QueueInfo _queueInfo = new QueueInfo();
        private int _lastGeneratedSecond;

        public InMemoryQueueInfo(InMemoryQueue inMemoryQueue)
        {
            _inMemoryQueue = inMemoryQueue;
            _lastGeneratedSecond = new TimeSpan(Stopwatch.GetTimestamp()).Seconds;
            InternalSetQueueInfo();
        }

        public QueueInfo GetQueueInfo()
        {
            var second = new TimeSpan(Stopwatch.GetTimestamp()).Seconds;
            if (_lastGeneratedSecond != second && _semaphore.Wait(0))
            {
                _lastGeneratedSecond = second;
                InternalSetQueueInfo();
                _semaphore.Release();
            }

            return _queueInfo!;
        }

        private void InternalSetQueueInfo()
        {
            int mainQueueSize = _inMemoryQueue.MainChannelCount;
            int retryQueueSize = _inMemoryQueue.RetryChannelCount;

            _queueInfo.CollectDate = DateTime.Now;
            _queueInfo.QueueName = _inMemoryQueue.Name;
            _queueInfo.QueueSize = mainQueueSize + retryQueueSize;
            _queueInfo.MainQueueSize = mainQueueSize;
            _queueInfo.RetryQueueSize = retryQueueSize;

            _queueInfo.ConcurrentConsumers = _inMemoryQueue.ConsumersCount;

            _queueInfo.AckCounter = _inMemoryQueue.Counters.AckCounter;
            _queueInfo.AckPerSecond = _inMemoryQueue.Counters.AckPerSecond;

            _queueInfo.NackCounter = _inMemoryQueue.Counters.NackCounter;
            _queueInfo.NackPerSecond = _inMemoryQueue.Counters.NackPerSecond;

            _queueInfo.PubCounter = _inMemoryQueue.Counters.PubCounter;
            _queueInfo.PubPerSecond = _inMemoryQueue.Counters.PubPerSecond;

            _queueInfo.RedeliverCounter = _inMemoryQueue.Counters.RedeliverCounter;
            _queueInfo.RedeliverPerSecond = _inMemoryQueue.Counters.RedeliverPerSecond;

            _queueInfo.DeliverCounter = _inMemoryQueue.Counters.DeliverCounter;
            _queueInfo.DeliverPerSecond = _inMemoryQueue.Counters.DeliverPerSecond;

            _queueInfo.AvgAckTimeMilliseconds = _inMemoryQueue.Counters.AvgConsumptionMs;

            _queueInfo.Consumers.Clear();
            _queueInfo.Consumers.AddRange(_inMemoryQueue.Consumers.Select(ParseConsumer));
        }

        private static ConsumerInfo ParseConsumer(QueueConsumerInfo info)
        {
            var _consumerInfo = new ConsumerInfo();
            _consumerInfo.Counters ??= new Models.Counters();

            _consumerInfo.Host = info.Host;
            _consumerInfo.Id = info.Id;
            _consumerInfo.Ip = info.Ip;
            _consumerInfo.Name = info.Name;
            _consumerInfo.Type = info.ConsumerType.ToString();

            _consumerInfo.Counters.AckCounter = info.Counters?.AckCounter ?? 0;
            _consumerInfo.Counters.AckPerSecond = info.Counters?.AckPerSecond ?? 0;
            _consumerInfo.Counters.AvgConsumptionMs = info.Counters?.AvgConsumptionMs ?? 0;
            _consumerInfo.Counters.DeliverCounter = info.Counters?.DeliverCounter ?? 0;
            _consumerInfo.Counters.DeliverPerSecond = info.Counters?.DeliverPerSecond ?? 0;
            _consumerInfo.Counters.NackCounter = info.Counters?.NackCounter ?? 0;
            _consumerInfo.Counters.NackPerSecond = info.Counters?.NackPerSecond ?? 0;
            _consumerInfo.Counters.Throttled = info.Counters?.Throttled ?? false;

            return _consumerInfo;
        }
    }
}
