namespace MemoryQueue.Base.Models
{
    public class QueueInfo
    {
        public required DateTime CollectDate { get; set; }
        public required string QueueName { get; set; }
        public required int QueueSize { get; set; }
        public required int MainQueueSize { get; set; }
        public required int RetryQueueSize { get; set; }
        public required int ConcurrentConsumers { get; set; }
        public required long AckCounter { get; set; }
        public required long AckPerSecond { get; set; }
        public required long NackCounter { get; set; }
        public required long NackPerSecond { get; set; }
        public required long PubCounter { get; set; }
        public required long PubPerSecond { get; set; }
        public required long RedeliverCounter { get; set; }
        public required long RedeliverPerSecond { get; set; }
        public required long DeliverCounter { get; set; }
        public required long DeliverPerSecond { get; set; }
        public required double AvgConsumptionMs { get; set; }
        public required List<ConsumerInfo> Consumers { get; set; } = new List<ConsumerInfo>();
    }

    public class ConsumerInfo
    {
        public required Counters Counters { get; set; }
        public required string Host { get; set; }
        public required string Id { get; set; }
        public required string Ip { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; }
    }

    public class Counters
    {
        public required long AckCounter { get; set; }
        public required long AckPerSecond { get; set; }
        public required double AvgConsumptionMs { get; set; }
        public required long DeliverCounter { get; set; }
        public required long DeliverPerSecond { get; set; }
        public required long NackCounter { get; set; }
        public required long NackPerSecond { get; set; }
        public required bool Throttled { get; set; }
    }
}
