using System.ComponentModel;
using Winforms.App.QueueInfoPropertyGrid;

namespace GrpcClient4.QueueInfoPropertyGrid;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoReplyMessagesPerSecondOutgoingProperty
{
    [DisplayName("Deliver")]
    [Description("Messages delivered to consumers per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long DeliveryPerSecond { get; internal set; }

    [DisplayName("Redeliver")]
    [Description("Messages redelivered to consumers per second")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long RedliveryPerSecond { get; internal set; }

    internal long OutgoingTotal { get; set; }
    public override string ToString()
    {
        return OutgoingTotal.ToString("N0");
    }
}

