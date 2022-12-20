using System.ComponentModel;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(QueueInfoReplyConsumerConverter))]
public sealed record QueueInfoReplyConsumer
{
    [DisplayName("Id")]
    [Description("Id of the consumer")]
    public Guid Id { get; internal set; } = Guid.Empty;

    [DisplayName("Name")]
    [Description("Name of the consumer")]
    public string Name { get; internal set; } = "";

    [DisplayName("Peer")]
    [Description("Reference peer of the connected consumer")]
    public string Peer { get; internal set; } = "";

    [DisplayName("Host")]
    [Description("Reference host of the connected consumer")]
    public string Host { get; internal set; } = "";

    [DisplayName("Type")]
    [Description("Type of the consumer")]
    public string Type { get; internal set; } = "";


    [DisplayName("Counters (Avg ms.)")]
    [Description("Consumer counters")]
    public QueueInfoConsumerCountersReply Counters { get; internal set; } = new QueueInfoConsumerCountersReply();

}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoConsumerCountersReply
{
    [DisplayName("Avg. Consume (ms)")]
    [Description("Average time in milliseconds for consuming a message (Ack | Nack)")]
    public string AvgConsumeMs { get; internal set; } = 0.ToString("N10");

    [DisplayName("Deliver p/ Second")]
    [Description("Messages delivered per second")]
    public string DeliverPerSecond { get; internal set; } = "0";

    [DisplayName("Redeliver p/ Second")]
    [Description("Messages redelivered per second")]
    public string RedeliverPerSecond { get; internal set; } = "0";

    [DisplayName("Ack p/ Second")]
    [Description("Ack per second")]
    public string AckPerSecond { get; internal set; } = "0";

    [DisplayName("Nack p/ Second")]
    [Description("Nack per second")]
    public string NackPerSecond { get; internal set; } = "0";

    [DisplayName("Total Acks")]
    [Description("Total acks")]
    public string AckCounter { get; internal set; } = "0";

    [DisplayName("Total Nacks")]
    [Description("Total nacks")]
    public string NackCounter { get; internal set; } = "0";

    [DisplayName("Total Deliver")]
    [Description("Total messages delivered")]
    public string DeliverCounter { get; internal set; } = "0";

    [DisplayName("Total Redeliver")]
    [Description("Total messages redelivered")]
    public string RedeliverCounter { get; internal set; } = "0";

    public override string ToString()
    {
        return AvgConsumeMs;
    }
}
