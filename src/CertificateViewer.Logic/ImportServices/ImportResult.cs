using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices;

public class ImportResult
{
    private ImportResult()
    {
    }
    
    public static ImportResult CreateSuccess(List<X509Certificate2> certificates) => new()
    {
        Certificates = certificates,
        Success = true
    };

    public static ImportResult CreateFail(Exception exception) => new()
    {
        Success = false,
        Error = exception
    };

    public List<X509Certificate2>? Certificates { get; set; }

    public bool Success { get; set; }
    public Exception? Error { get; set; }
}