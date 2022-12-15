using System.ComponentModel;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoReplyMessagesPerSecondOutgoingProperty
{
    [DisplayName("Deliver")]
    [Description("Messages delivered to consumers per second")]
    public string DeliveryPerSecond { get; internal set; } = string.Empty;

    [DisplayName("Redeliver")]
    [Description("Messages redelivered to consumers per second")]
    public string RedliveryPerSecond { get; internal set; } = string.Empty;

    internal string OutgoingTotal { get; set; } = string.Empty;
    public override string ToString()
    {
        return OutgoingTotal;
    }
}

