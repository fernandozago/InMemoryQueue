using System.ComponentModel;
using System.Drawing.Design;
using Winforms.App.QueueInfoPropertyGrid;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoReplyPropertyGrid
{
    #region Consumers
    [Category("Consumers")]
    [DisplayName("Active Consumers")]
    [Description("List of active consumers")]
    [Editor(typeof(DisableEditor), typeof(UITypeEditor))]
    public ConsumerCollection Consumers { get; internal set; } = new();

    [Category("Consumers")]
    [DisplayName("ETA")]
    [Description("Estimated time remaining to the completion of all items")]
    public string ETA { get; internal set; } = string.Empty;

    [Category("Consumers")]
    [DisplayName("Avg. Consume (ms)")]
    [Description("Average time in milliseconds for consuming a message (Ack | Nack)")]
    [TypeConverter(typeof(CustomDoubleTypeConverter))]
    public double AvgConsumptionMS { get; internal set; }
    #endregion

    #region Messages p/ Second

    [Category("Messages p/ Second")]
    [DisplayName("Incoming")]
    [Description("Incoming messages to queue")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long PubPerSecond { get; internal set; }

    [Category("Messages p/ Second")]
    [DisplayName("Outgoing")]
    [Description("Outgoing messages from queue")]
    public QueueInfoReplyMessagesPerSecondOutgoingProperty Outgoing { get; internal set; } = new();

    [Category("Messages p/ Second")]
    [DisplayName("Ack")]
    [Description("Message acks per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long AckPerSecond { get; internal set; }

    [Category("Messages p/ Second")]
    [DisplayName("Nack")]
    [Description("Message nacks per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long NackPerSecond { get; internal set; }
    #endregion

    #region Queue Size
    [Category("Queue")]
    [DisplayName("Queue Name")]
    [Description("Queue Name")]
    public string QueueName { get; internal set; } = string.Empty;

    [Category("Queue")]
    [DisplayName("Pending")]
    [Description("All pending messages")]
    public QueueInfoReplyMessagesPerSecondQueueSizeProperty TotalQueueSize { get; internal set; } = new();
    #endregion

    #region Totals
    [Category("Totals")]
    [DisplayName("Publication")]
    [Description("Messages added to the queue")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long TotalPub { get; internal set; }

    [Category("Totals")]
    [DisplayName("Delivery")]
    [Description("Messages delivered to consumers")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long TotalDelivery { get; internal set; }

    [Category("Totals")]
    [DisplayName("Redelivery")]
    [Description("Messages redelivered to consumers")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long TotalRedelivery { get; internal set; }

    [Category("Totals")]
    [DisplayName("Acks")]
    [Description("Total of received acks")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long TotalAck { get; internal set; }

    [Category("Totals")]
    [DisplayName("Nacks")]
    [Description("Total of received nacks")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long TotalNack { get; internal set; }
    #endregion
}

