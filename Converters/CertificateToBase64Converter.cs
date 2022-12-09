using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MugenMvvmToolkit;

namespace CertificateViewer.Converters;

[ValueConversion(typeof(CertificateVm),typeof(string))]
public class CertificateToBase64Converter: IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value.GetType() != typeof(CertificateVm))
        {
            throw new ArgumentException("Certificate converter only supports X509Certificate2 types");
        }
        var vm = value as CertificateVm;
        var certificate = vm?.Certificate;
        if (certificate == null)
        {
            return string.Empty;
        }
        StringBuilder builder = new();
        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        System.Convert.ToBase64String(certificate.RawData)
            .Chunk(64)
            .ForEach(x=>builder.AppendLine(new string(x)));
        builder.AppendLine("-----END CERTIFICATE-----");
        return builder.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}