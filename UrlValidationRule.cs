using System;
using System.Globalization;
using System.Windows.Controls;

namespace CertificateViewerPlayground;

public class UrlValidationRule : ValidationRule
{
    public bool HttpsOnly { get; set; }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string)
        {
            return new ValidationResult(false, "Value must be string");
        }
        try
        {
            var uri = new Uri((string)value);
            if (HttpsOnly == false)
            {
                return ValidationResult.ValidResult;
            }
            if (uri.Scheme == "https")
            {
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, "Enter HTTPS server address");
        }
        catch (Exception e)
        {
            return new ValidationResult(false, e.Message);
        }
    }
}
