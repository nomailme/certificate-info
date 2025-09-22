using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;

namespace CertificateViewer.Components.CertificateDetails;

public partial class CertificateDetailsPanel : UserControl
{
    public static readonly DirectProperty<CertificateDetailsPanel, X509Certificate2?> CertificateProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, X509Certificate2?>(
            nameof(Certificate),
            o => o.Certificate,
            (o, v) => o.Certificate = v);

    public static readonly DirectProperty<CertificateDetailsPanel, bool?> ShowRawProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, bool?>(
            nameof(ShowRaw),
            o => o.ShowRaw,
            (o, v) => o.ShowRaw = v);

    private X509Certificate2? _certificate;
    private bool? _showRaw = false;

    static CertificateDetailsPanel() => AffectsRender<CertificateDetailsPanel>(CertificateProperty);

    public CertificateDetailsPanel() => InitializeComponent();

    public bool? ShowRaw
    {
        get => _showRaw;
        set => SetAndRaise(ShowRawProperty, ref _showRaw, value);
    }

    public X509Certificate2? Certificate
    {
        get => _certificate;
        set => SetAndRaise(CertificateProperty, ref _certificate, value);
    }
}
