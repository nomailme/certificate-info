using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class CertificateToBase64Converter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var certificate = value as X509Certificate2;
        if (certificate == null)
        {
            return string.Empty;
        }
        StringBuilder builder = new();
        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        System.Convert.ToBase64String(certificate.RawData)
            .Chunk(64)
            .ToList()
            .ForEach(x => builder.AppendLine(new string(x)));
        builder.AppendLine("-----END CERTIFICATE-----");
        return builder.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
