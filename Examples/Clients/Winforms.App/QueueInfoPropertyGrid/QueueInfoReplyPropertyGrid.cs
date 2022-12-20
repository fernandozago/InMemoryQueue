using System.ComponentModel;
using System.Drawing.Design;

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
    public string AvgConsumptionMS { get; internal set; } = string.Empty;
    #endregion

    #region Messages p/ Second

    [Category("Messages p/ Second")]
    [DisplayName("Incoming")]
    [Description("Incoming messages to queue")]
    public string PubPerSecond { get; internal set; } = string.Empty;

    [Category("Messages p/ Second")]
    [DisplayName("Outgoing")]
    [Description("Outgoing messages from queue")]
    public QueueInfoReplyMessagesPerSecondOutgoingProperty Outgoing { get; internal set; } = new();

    [Category("Messages p/ Second")]
    [DisplayName("Ack")]
    [Description("Message acks per second")]
    public string AckPerSecond { get; internal set; } = string.Empty;

    [Category("Messages p/ Second")]
    [DisplayName("Nack")]
    [Description("Message nacks per second")]
    public string NackPerSecond { get; internal set; } = string.Empty;
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
    public string TotalPub { get; internal set; } = string.Empty;

    [Category("Totals")]
    [DisplayName("Delivery")]
    [Description("Messages delivered to consumers")]
    public string TotalDelivery { get; internal set; } = string.Empty;

    [Category("Totals")]
    [DisplayName("Redelivery")]
    [Description("Messages redelivered to consumers")]
    public string TotalRedelivery { get; internal set; } = string.Empty;

    [Category("Totals")]
    [DisplayName("Acks")]
    [Description("Total of received acks")]
    public string TotalAck { get; internal set; } = string.Empty;

    [Category("Totals")]
    [DisplayName("Nacks")]
    [Description("Total of received nacks")]
    public string TotalNack { get; internal set; } = string.Empty;
    #endregion
}

