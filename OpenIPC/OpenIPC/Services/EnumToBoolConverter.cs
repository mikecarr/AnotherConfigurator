using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenIPC.Services;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && value.GetType().IsEnum)
        {
            return (int)value == (int)parameter;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType.IsEnum)
        {
            if (value is bool boolValue)
            {
                if (boolValue)
                {
                    return Enum.Parse(targetType, parameter.ToString());
                }
                else
                {
                    return Enum.Parse(targetType, "None"); // or some other default value
                }
            }
        }
        throw new ArgumentException("Unsupported type", nameof(value));
    }
}