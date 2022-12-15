using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return true;
        }
        return !(bool)value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return true;
        }
        return !(bool)value;
    }
}
