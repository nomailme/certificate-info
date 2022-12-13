using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer;

public sealed class CertificateVm
{
    public CertificateVm(X509Certificate2 certificate) => Certificate = certificate;

    public string Subject => $"Subject {Certificate?.Subject}";
    public string Issuer => $"Issuer {Certificate?.Issuer}";

    public string Validity => $"{Certificate?.NotBefore:d}-{Certificate?.NotAfter:d}";

    public X509Certificate2 Certificate { get; set; }
}
