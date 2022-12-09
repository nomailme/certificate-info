using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer;

public class RootCertificateStore
{
    public bool UseSystemRootStore { get; set; } = true;

    public X509Certificate2Collection RootCertificates { get; set; } = new();

    public void Add(params X509Certificate2[] certificate) => RootCertificates.AddRange(certificate);

    public void Remove(params X509Certificate2[] certificate) => RootCertificates.RemoveRange(certificate);

    public void Clear() => RootCertificates.Clear();
}

