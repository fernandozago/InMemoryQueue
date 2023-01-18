using MemoryQueue.Base.Models;
using MemoryQueue.Transports.SignalR;
using System.Xml.Linq;

namespace MemoryQueue.SignalR.Parsers
{
    public static class QueueInfoParser
    {

        public static QueueInfoReply ToReply(this QueueInfo info)
        {
            var reply = new QueueInfoReply()
            {
                CollectDate = info.CollectDate,

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

                Consumers = ParseConsumers(info.Consumers).ToList()
            };

            return reply;
        }

        private static IEnumerable<ConsumerInfoReply> ParseConsumers(List<ConsumerInfo> consumers)
        {
            return consumers.Select(info => new ConsumerInfoReply()
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
            });
        }

    }
}
