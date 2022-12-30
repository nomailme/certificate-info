using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CertificateViewer.Converters;

public class BooleanToBackgroundStyle : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return Brushes.Gray;
        }
        if ((bool)value)
        {

            return new SolidColorBrush(Color.Parse("#1b5e20"));
        }
        return new SolidColorBrush(Color.Parse("#8e0000"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
