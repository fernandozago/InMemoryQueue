using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Winforms.App.QueueInfoPropertyGrid;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(QueueInfoReplyConsumerConverter))]
public sealed record QueueInfoReplyConsumer
{
    [DisplayName("Id")]
    [Description("Id of the consumer")]
    public string Id { get; internal set; } = "";

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
    [TypeConverter(typeof(CustomDoubleTypeConverter))]
    public double AvgConsumeMs { get; internal set; }

    [DisplayName("Deliver p/ Second")]
    [Description("Messages delivered per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long DeliverPerSecond { get; internal set; }

    [DisplayName("Ack p/ Second")]
    [Description("Ack per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long AckPerSecond { get; internal set; }

    [DisplayName("Nack p/ Second")]
    [Description("Nack per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long NackPerSecond { get; internal set; }

    [DisplayName("Total Acks")]
    [Description("Total acks")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long AckCounter { get; internal set; }

    [DisplayName("Total Nacks")]
    [Description("Total nacks")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long NackCounter { get; internal set; }

    [DisplayName("Total Deliver")]
    [Description("Total messages delivered")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long DeliverCounter { get; internal set; }

    [DisplayName("Throttled")]
    [Description("Is this consumer being throttled down")]
    public bool Throttled { get; internal set; }

    public override string ToString()
    {
        return AvgConsumeMs.ToString("N10");
    }
}
