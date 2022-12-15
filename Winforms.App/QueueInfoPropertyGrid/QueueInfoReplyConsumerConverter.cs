using System.ComponentModel;
using System.Globalization;

namespace GrpcClient4.QueueInfoPropertyGrid;
internal class QueueInfoReplyConsumerConverter : ExpandableObjectConverter
{
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is QueueInfoReplyConsumer emp)
        {
            return emp.Name;
        }
        return base.ConvertTo(context, culture, value, destinationType);
        //if (destinationType == typeof(string) && value is QueueInfoReplyConsumer emp)
        //{
        //    return emp.Name;
        //}
    }
}

