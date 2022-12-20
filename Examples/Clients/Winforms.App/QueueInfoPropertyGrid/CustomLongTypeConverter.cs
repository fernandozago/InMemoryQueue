using System.ComponentModel;
using System.Globalization;

namespace Winforms.App.QueueInfoPropertyGrid
{
    public class CustomLongTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context,
                                            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context,
            CultureInfo? culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context,
            CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is not null && destinationType == typeof(string))
                return ((long)value).ToString("N0", culture);

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class CustomDoubleTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context,
                                            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context,
            CultureInfo? culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context,
            CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is not null && destinationType == typeof(string))
                return ((double)value).ToString("N10", culture);

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
