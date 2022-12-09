using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CertificateViewer.Converters;

[ValueConversion(typeof(bool),typeof(Brush))]
public class BooleanToBackgroundStyle: IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return Brushes.Gray;
        }
        if ((bool)value)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1b5e20"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8e0000"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}