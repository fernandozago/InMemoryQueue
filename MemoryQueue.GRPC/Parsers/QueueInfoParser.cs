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

                AvgConsumptionMs = info.AvgConsumptionMs,
            };
            reply.Consumers.AddRange(info.Consumers.Select(ParseConsumer));
            return reply;
        }

        private static ConsumerInfoReply ParseConsumer(ConsumerInfo info)
        {
            return new ConsumerInfoReply()
            {
                Host = info.Host,
                Id = info.Id,
                Ip = info.Ip,
                Name = info.Name,
                Type = info.Type,
                Counters = new ConsumerCounters()
                {
                    AckCounter = info.Counters.AckCounter,
                    AckPerSecond = info.Counters.AckPerSecond,
                    AvgConsumptionMs = info.Counters.AvgConsumptionMs,
                    DeliverCounter = info.Counters.DeliverCounter,
                    DeliverPerSecond = info.Counters.DeliverPerSecond,
                    NackCounter = info.Counters.NackCounter,
                    NackPerSecond = info.Counters.NackPerSecond,
                    Throttled = info.Counters.Throttled
                }
            };
        }

    }
}
