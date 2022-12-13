using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.CertificateImporters;

public record OperationResult
{
    private OperationResult()
    {
    }

    public List<X509Certificate2>? Certificates { get; init; }

    public CertificateType Type { get; init; }

    public bool Success { get; set; }

    public Exception? Error { get; set; }

    public static OperationResult CreateSuccess(List<X509Certificate2> certificates, CertificateType type) => new()
    {
        Certificates = certificates,
        Type = type,
        Success = true
    };

    public static OperationResult CreateFail(Exception exception) => new()
    {
        Success = false,
        Error = exception
    };
}
