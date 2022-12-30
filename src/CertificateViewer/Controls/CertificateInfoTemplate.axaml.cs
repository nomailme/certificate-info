using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace CertificateViewer.Controls;

public class CertificateInfoTemplate : TemplatedControl
{
    public static readonly DirectProperty<CertificateInfoTemplate, X509Certificate2?> CertificateProperty =
        AvaloniaProperty.RegisterDirect<CertificateInfoTemplate, X509Certificate2?>(nameof(Certificate),o=>o.Certificate,(o,v)=> o.Certificate = v);
    private X509Certificate2? certificate;

    public X509Certificate2? Certificate
    {
        get => certificate;
        set => SetAndRaise(CertificateProperty, ref certificate, value);
    }
}
