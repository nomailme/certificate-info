using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace CertificateViewer.Templates;

public class CertificateInfoTemplate : TemplatedControl
{
    public static readonly DirectProperty<CertificateInfoTemplate, CertificateVm?> CertificateProperty =
        AvaloniaProperty.RegisterDirect<CertificateInfoTemplate, CertificateVm?>(nameof(Certificate),o=>o.Certificate,(o,v)=> o.Certificate = v);
    private CertificateVm? certificate;

    public CertificateVm? Certificate
    {
        get => certificate;
        set => SetAndRaise(CertificateProperty, ref certificate, value);
    }
}
