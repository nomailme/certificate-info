using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;

namespace CertificateViewer.Controls.Dialogs;

public record DialogResult
{
    public enum OperationResult
    {
        Success,
        Failure,
        Canceled
    }

    public List<X509Certificate2> Certificates { get; init; } = new();

    public CertificateType Type { get; init; }

    public OperationResult Success { get; set; }

    public Exception? Error { get; set; }

    public static DialogResult CreateFail(Exception exception) => new()
    {
        Success = OperationResult.Failure, Error = exception
    };

    public static DialogResult OperationCanceled() => new() { Success = OperationResult.Canceled };
}
