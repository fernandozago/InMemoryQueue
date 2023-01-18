using System;
using System.Collections.Generic;

namespace MemoryQueue.Transports.SignalR;

public sealed class QueueInfoReply
{
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
    public required List<ConsumerInfoReply> Consumers { get; set; } = new List<ConsumerInfoReply>();
    public required DateTime CollectDate { get; set; }
}
