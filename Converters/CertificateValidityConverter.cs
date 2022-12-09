using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Data;

namespace CertificateViewer.Converters;

[ValueConversion(typeof(X509Certificate2), typeof(string))]
public class CertificateValidityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value.GetType() != typeof(X509Certificate2))
        {
            throw new ArgumentException("Certificate converter only supports X509Certificate2 types");
        }
        var certificate = value as X509Certificate2;
        if (certificate == null)
        {
            return string.Empty;
        }
        return $"{certificate?.NotBefore:d}-{certificate?.NotAfter:d}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
