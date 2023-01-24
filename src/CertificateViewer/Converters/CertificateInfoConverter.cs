using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class CertificateInfoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value is not X509Certificate2 certificate)
        {
            throw new NotSupportedException("Only X509Certificate2 is supported");
        }
        var result = certificate.ToString(true);
        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
