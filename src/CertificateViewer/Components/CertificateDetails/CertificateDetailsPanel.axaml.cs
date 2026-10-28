using System;
using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;
using CertificateViewer.Logic;

namespace CertificateViewer.Components.CertificateDetails;

public partial class CertificateDetailsPanel : UserControl
{
    public static readonly DirectProperty<CertificateDetailsPanel, X509Certificate2?> CertificateProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, X509Certificate2?>(
            nameof(Certificate),
            o => o.Certificate,
            (o, v) => o.Certificate = v);

    public static readonly DirectProperty<CertificateDetailsPanel, ViewMode> ViewModeProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, ViewMode>(
            nameof(ViewMode),
            o => o.ViewMode,
            (o, v) => o.ViewMode = v);

    private X509Certificate2? _certificate;
    private ViewMode _viewMode = ViewMode.General;

    static CertificateDetailsPanel() => AffectsRender<CertificateDetailsPanel>(CertificateProperty);

    public CertificateDetailsPanel() => InitializeComponent();

    public ViewMode[] ViewModes { get; } = Enum.GetValues<ViewMode>();

    public ViewMode ViewMode
    {
        get => _viewMode;
        set => SetAndRaise(ViewModeProperty, ref _viewMode, value);
    }

    public X509Certificate2? Certificate
    {
        get => _certificate;
        set => SetAndRaise(CertificateProperty, ref _certificate, value);
    }
}
