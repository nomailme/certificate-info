using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class CertificateTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value.GetType() != typeof(CertificateType))
        {
            throw new ArgumentException("Certificate converter only supports CertificateType types");
        }
        var certificateType = value is CertificateType type ? type : CertificateType.Unknown;
        return certificateType switch
        {
            CertificateType.Der => "DER",
            CertificateType.Pem => "PEM",
            CertificateType.Web => "WEB",
            _ => certificateType.ToString("G")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
