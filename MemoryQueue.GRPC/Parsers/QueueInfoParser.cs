using Google.Protobuf.WellKnownTypes;
using MemoryQueue.Base.Models;
using MemoryQueue.Transports.GRPC;

namespace MemoryQueue.GRPC.Parsers
{
    public static class QueueInfoParser
    {

        public static QueueInfoReply ToReply(this QueueInfo info)
        {
            var reply = new QueueInfoReply()
            {
                CollectDate = Timestamp.FromDateTime(info.CollectDate.ToUniversalTime()),
                QueueName = info.QueueName,
                QueueSize = info.QueueSize,
                MainQueueSize = info.MainQueueSize,
                RetryQueueSize = info.RetryQueueSize,

                ConcurrentConsumers = info.ConcurrentConsumers,

                AckCounter = info.AckCounter,
                AckPerSecond = info.AckPerSecond,

                NackCounter = info.NackCounter,
                NackPerSecond = info.NackPerSecond,

                PubCounter = info.PubCounter,
                PubPerSecond = info.PubPerSecond,

                RedeliverCounter = info.RedeliverCounter,
                RedeliverPerSecond = info.RedeliverPerSecond,

                DeliverCounter = info.DeliverCounter,
                DeliverPerSecond = info.DeliverPerSecond,

                AvgAckTimeMilliseconds = info.AvgAckTimeMilliseconds
            };
            reply.Consumers.AddRange(info.Consumers.Select(ParseConsumer));

            return reply;
        }

        private static ConsumerInfoReply ParseConsumer(ConsumerInfo info)
        {
            var _consumerInfo = new ConsumerInfoReply();
            _consumerInfo.Counters ??= new ConsumerCounters();

            _consumerInfo.Host = info.Host;
            _consumerInfo.Id = info.Id;
            _consumerInfo.Ip = info.Ip;
            _consumerInfo.Name = info.Name;
            _consumerInfo.Type = info.Type;

            _consumerInfo.Counters.AckCounter = info.Counters.AckCounter;
            _consumerInfo.Counters.AckPerSecond = info.Counters.AckPerSecond;
            _consumerInfo.Counters.AvgConsumptionMs = info.Counters.AvgConsumptionMs;
            _consumerInfo.Counters.DeliverCounter = info.Counters.DeliverCounter;
            _consumerInfo.Counters.DeliverPerSecond = info.Counters.DeliverPerSecond;
            _consumerInfo.Counters.NackCounter = info.Counters.NackCounter;
            _consumerInfo.Counters.NackPerSecond = info.Counters.NackPerSecond;
            _consumerInfo.Counters.Throttled = info.Counters.Throttled;

            return _consumerInfo;
        }

    }
}
