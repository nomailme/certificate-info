using System;
using System.Globalization;
using System.Windows.Data;

namespace CertificateViewer.Converters;

[ValueConversion(typeof(CertificateType),typeof(string))]
public class CertificateTypeConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value.GetType() != typeof(CertificateType))
        {
            throw new ArgumentException("Certificate converter only supports CertificateType types");
        }
        var certificateType = value is CertificateType ? (CertificateType)value : CertificateType.Unknown;
        if (certificateType == CertificateType.Der)
        {
            return "DER";
        }
        if (certificateType == CertificateType.Pem)
        {
            return "PEM";
        }
        if (certificateType == CertificateType.Web)
        {
            return "WEB";
        }
        return certificateType.ToString("G");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
