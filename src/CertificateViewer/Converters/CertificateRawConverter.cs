using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class CertificateRawConverter:IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {

        if (value == null)
        {
            return new List<string>();
        }

        if (value is not X509Certificate2 cert)
        {
            throw new ArgumentException("Value is not a certificate");
        }

        return cert.ToString(true);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();


}
