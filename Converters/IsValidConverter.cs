using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class IsValidConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "Empty";
        }
        if (value is not bool)
        {
            throw new NotSupportedException("Only string is acceptable");
        }
        if ((bool)value)
        {

            return "Valid";
        }
        return "Invalid";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
