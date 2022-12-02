using System;
using System.Globalization;
using System.Windows.Data;

namespace CertificateViewerPlayground.Converters;

[ValueConversion(typeof(CertificateVm),typeof(string))]
public class CertificateInfoConverter: IValueConverter
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
        if (certificate is null)
        {
            return string.Empty;
        }
        var result = certificate.ToString(true);
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}