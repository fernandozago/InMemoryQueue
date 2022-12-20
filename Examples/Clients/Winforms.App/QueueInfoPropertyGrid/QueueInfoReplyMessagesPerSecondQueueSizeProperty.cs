using System.ComponentModel;
using Winforms.App.QueueInfoPropertyGrid;

namespace GrpcClient4.QueueInfoPropertyGrid;
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoReplyMessagesPerSecondQueueSizeProperty
{
    [DisplayName("Main")]
    [Description("Pending messages into the main queue")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long MainQueueSize { get; internal set; }

    [DisplayName("Retry")]
    [Description("Pending messages into the retry queue")]
    [TypeConverter(typeof(CustomLongTypeConverter))]
    public long RetryQueueSize { get; internal set; }

    internal long QueueSize { get; set; }
    public override string ToString()
    {
        return QueueSize.ToString("N0");
    }
}

