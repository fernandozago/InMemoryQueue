using System.ComponentModel;

namespace GrpcClient4.QueueInfoPropertyGrid;
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed record QueueInfoReplyMessagesPerSecondQueueSizeProperty
{
    [DisplayName("Main")]
    [Description("Pending messages into the main queue")]
    public string MainQueueSize { get; internal set; } = string.Empty;

    [DisplayName("Retry")]
    [Description("Pending messages into the retry queue")]
    public string RetryQueueSize { get; internal set; } = string.Empty;

    internal string QueueSize { get; set; } = string.Empty;
    public override string ToString()
    {
        return QueueSize;
    }
}

