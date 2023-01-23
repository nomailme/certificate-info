using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class CertificateValidityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var certificate2 = value as X509Certificate2;
        if (certificate2 == null)
        {
            throw new NotSupportedException("Only X509Certificate2 is supported");
        }
        var certificate = certificate2;
        return $"{certificate.NotBefore} - {certificate.NotAfter}";

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
