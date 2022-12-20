using MemoryQueue.Transports.GRPC;

namespace GrpcClient4.QueueInfoPropertyGrid;

public static class QueueInfoReplyPropertyGridExtensions
{
    public static void Convert(this QueueInfoReply reply, ref QueueInfoReplyPropertyGrid refVal)
    {
        if (refVal != null)
        {
            refVal.QueueName = reply.QueueName;
            refVal.AckPerSecond = reply.AckPerSecond.ToString("N0");
            refVal.NackPerSecond = reply.NackPerSecond.ToString("N0");
            refVal.Outgoing.RedliveryPerSecond = reply.RedeliverPerSecond.ToString("N0");
            refVal.Outgoing.DeliveryPerSecond = reply.DeliverPerSecond.ToString("N0");
            refVal.Outgoing.OutgoingTotal = (reply.RedeliverPerSecond + reply.DeliverPerSecond).ToString("N0");
            refVal.AvgConsumptionMS = reply.AvgAckTimeMilliseconds.ToString("N10");

            refVal.PubPerSecond = reply.PubPerSecond.ToString("N0");
            refVal.TotalQueueSize.QueueSize = reply.QueueSize.ToString("N0");
            refVal.TotalQueueSize.MainQueueSize = reply.MainQueueSize.ToString("N0");
            refVal.TotalQueueSize.RetryQueueSize = reply.RetryQueueSize.ToString("N0");
            MergeConsumers(reply, refVal);

            refVal.TotalAck = reply.AckCounter.ToString("N0");
            refVal.TotalNack = reply.NackCounter.ToString("N0");
            refVal.TotalDelivery = reply.DeliverCounter.ToString("N0");
            refVal.TotalRedelivery = reply.RedeliverCounter.ToString("N0");
            refVal.TotalPub = reply.PubCounter.ToString("N0");

            refVal.ETA = GetEta(reply);
        }
    }

    private static void MergeConsumers(QueueInfoReply reply, QueueInfoReplyPropertyGrid refVal)
    {
        refVal.Consumers.RemoveRemovedConsumers(reply.Consumers.Select(x => Guid.Parse(x.Id)));
        foreach (var consumer in reply.Consumers)
        {
            var result = new QueueInfoReplyConsumer()
            {
                Id = Guid.Parse(consumer.Id),
                Peer = consumer.Ip,
                Name = consumer.Name,
                Host = consumer.Host,
                Type = consumer.Type,
            };

            result.Counters.AvgConsumeMs = consumer.Counters.AvgAckTimeMilliseconds.ToString("N10");
            result.Counters.DeliverPerSecond = consumer.Counters.DeliverPerSecond.ToString("N0");
            result.Counters.RedeliverPerSecond = consumer.Counters.RedeliverPerSecond.ToString("N0");

            result.Counters.DeliverCounter = consumer.Counters.DeliverCounter.ToString("N0");
            result.Counters.RedeliverCounter = consumer.Counters.RedeliverCounter.ToString("N0");

            result.Counters.AckPerSecond = consumer.Counters.AckPerSecond.ToString("N0");
            result.Counters.NackPerSecond = consumer.Counters.NackPerSecond.ToString("N0");

            result.Counters.NackCounter = consumer.Counters.NackCounter.ToString("N0");
            result.Counters.AckCounter = consumer.Counters.AckCounter.ToString("N0");

            refVal.Consumers.AddOrUpdate(result);
        }
    }

    private const string TIMEOUT_ZERO = "00:00:00";
    private const string TIMEOUT_INFINITE = "Infinite";
    private static string GetEta(QueueInfoReply reply)
    {
        if (reply.AckPerSecond > 0 && reply.QueueSize > 0)
        {
            return TimeSpan.FromSeconds(Math.Ceiling(reply.QueueSize / (double)reply.AckPerSecond)).ToString();
        }
        else if (reply.QueueSize == 0)
        {
            return TIMEOUT_ZERO;
        }
        else
        {
            return TIMEOUT_INFINITE;
        }
    }
}

