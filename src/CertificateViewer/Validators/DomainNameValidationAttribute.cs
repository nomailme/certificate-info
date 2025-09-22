using System;
using System.ComponentModel.DataAnnotations;

namespace CertificateViewer.Validators;

public class DomainNameValidationAttribute() : ValidationAttribute(DefaultErrorMessage)
{
    private static readonly string DefaultErrorMessage = "Please enter a valid address";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string)
        {
            return new ValidationResult("Provide a string representation of a base URI");
        }

        var result = Uri.CheckHostName((string)value);
        if (result != UriHostNameType.Unknown)
        {
            return new ValidationResult("Provided address is not a valid address");
        }

        return ValidationResult.Success;
    }
}
