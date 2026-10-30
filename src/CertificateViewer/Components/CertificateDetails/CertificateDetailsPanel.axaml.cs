using Avalonia.Controls;

namespace CertificateViewer.Components.CertificateDetails;

/// <summary>
///     Dumb view for the certificate details: all state lives on
///     <see cref="ViewModels.CertificateDetailsViewModel" />, whose instance is the panel's DataContext
///     (set by the host). The code-behind only initializes the component.
/// </summary>
public partial class CertificateDetailsPanel : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="CertificateDetailsPanel" /> class.</summary>
    public CertificateDetailsPanel()
    {
        InitializeComponent();
    }
}
