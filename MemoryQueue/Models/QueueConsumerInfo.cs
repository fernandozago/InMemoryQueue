using MemoryQueue.Counters;
using MemoryQueue.Transports.GRPC;

namespace MemoryQueue.Models
{
    public sealed record QueueConsumerInfo
    {
        public QueueConsumerInfo(QueueConsumerType consumerType)
        {
            ConsumerType = consumerType;
        }

        internal ReaderConsumptionCounter? Counters { get; set; }
        public QueueConsumerType ConsumerType { get; private set; }

        required public string Id { get; set; }
        required public string Name { get; set; }
        required public string Ip { get; set; }
        required public string Host { get; set; }


        private ConsumerInfoReply _consumerInfo;
        public ConsumerInfoReply ToGrpc()
        {
            _consumerInfo ??= new ConsumerInfoReply();
            _consumerInfo.Counters ??= new ConsumerCounters();

            _consumerInfo.Host = Host;
            _consumerInfo.Id = Id;
            _consumerInfo.Ip = Ip;
            _consumerInfo.Name = Name;
            _consumerInfo.Type = ConsumerType.ToString();
            
            _consumerInfo.Counters.AckCounter = Counters?.AckCounter ?? 0;
            _consumerInfo.Counters.AckPerSecond = Counters?.AckPerSecond ?? 0;
            _consumerInfo.Counters.AvgAckTimeMilliseconds = Counters?.AvgAckTimeMilliseconds ?? 0;
            _consumerInfo.Counters.DeliverCounter = Counters?.DeliverCounter ?? 0;
            _consumerInfo.Counters.DeliverPerSecond = Counters?.DeliverPerSecond ?? 0;
            _consumerInfo.Counters.NackCounter = Counters?.NackPerSecond ?? 0;
            _consumerInfo.Counters.NackPerSecond = Counters?.NackPerSecond ?? 0;

            return _consumerInfo;
        }
    }
}
