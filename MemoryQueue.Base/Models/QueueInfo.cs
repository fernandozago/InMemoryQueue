namespace MemoryQueue.Base.Models
{
    public class QueueInfo
    {
        public DateTime CollectDate { get; set; }
        public string QueueName { get; set; }
        public int QueueSize { get; set; }
        public int MainQueueSize { get; set; }
        public int RetryQueueSize { get; set; }
        public int ConcurrentConsumers { get; set; }
        public long AckCounter { get; set; }
        public long AckPerSecond { get; set; }
        public long NackCounter { get; set; }
        public long NackPerSecond { get; set; }
        public long PubCounter { get; set; }
        public long PubPerSecond { get; set; }
        public long RedeliverCounter { get; set; }
        public long RedeliverPerSecond { get; set; }
        public long DeliverCounter { get; set; }
        public long DeliverPerSecond { get; set; }
        public double AvgAckTimeMilliseconds { get; set; }
        public List<ConsumerInfo> Consumers { get; set; } = new List<ConsumerInfo>();
    }

    public class ConsumerInfo
    {
        public Counters Counters { get; set; }
        public string Host { get; set; }
        public string Id { get; set; }
        public string Ip { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class Counters
    {
        public long AckCounter { get; set; }
        public long AckPerSecond { get; set; }
        public double AvgConsumptionMs { get; set; }
        public long DeliverCounter { get; set; }
        public long DeliverPerSecond { get; set; }
        public long NackCounter { get; set; }
        public long NackPerSecond { get; set; }
        public bool Throttled { get; set; }
    }
}
