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

    [DisplayName("Updated At")]
    [Description("Last consumer update time")]
    public string UpdatedAt { get; internal set; } = DateTime.Now.ToString("HH:mm:ss");
}

