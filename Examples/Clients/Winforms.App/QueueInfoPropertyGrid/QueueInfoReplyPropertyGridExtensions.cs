using MemoryQueue.Transports.GRPC;

namespace GrpcClient4.QueueInfoPropertyGrid;

public static class QueueInfoReplyPropertyGridExtensions
{
    public static void Convert(this QueueInfoReply reply, ref QueueInfoReplyPropertyGrid refVal)
    {
        if (refVal != null)
        {
            refVal.ETA = GetEta(reply);

            refVal.QueueName = reply.QueueName;
            refVal.AckPerSecond = reply.AckPerSecond;
            refVal.NackPerSecond = reply.NackPerSecond;
            refVal.Outgoing.RedliveryPerSecond = reply.RedeliverPerSecond;
            refVal.Outgoing.DeliveryPerSecond = reply.DeliverPerSecond;
            refVal.Outgoing.OutgoingTotal = reply.RedeliverPerSecond + reply.DeliverPerSecond;
            refVal.AvgConsumptionMS = reply.AvgAckTimeMilliseconds;

            refVal.PubPerSecond = reply.PubPerSecond;
            refVal.TotalQueueSize.QueueSize = reply.QueueSize;
            refVal.TotalQueueSize.MainQueueSize = reply.MainQueueSize;
            refVal.TotalQueueSize.RetryQueueSize = reply.RetryQueueSize;

            refVal.TotalAck = reply.AckCounter;
            refVal.TotalNack = reply.NackCounter;
            refVal.TotalDelivery = reply.DeliverCounter;
            refVal.TotalRedelivery = reply.RedeliverCounter;
            refVal.TotalPub = reply.PubCounter;

            MergeConsumers(reply, refVal);
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

            result.Counters.AvgConsumeMs = consumer.Counters.AvgAckTimeMilliseconds;

            result.Counters.DeliverPerSecond = consumer.Counters.DeliverPerSecond;
            result.Counters.AckPerSecond = consumer.Counters.AckPerSecond;
            result.Counters.NackPerSecond = consumer.Counters.NackPerSecond;

            result.Counters.DeliverCounter = consumer.Counters.DeliverCounter;
            result.Counters.NackCounter = consumer.Counters.NackCounter;
            result.Counters.AckCounter = consumer.Counters.AckCounter;

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

