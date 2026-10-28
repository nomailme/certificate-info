using Avalonia.Controls;

namespace CertificateViewer.Components.CertificateChain;

/// <summary>
///     Dumb view for the certificate chain: all state lives on
///     <see cref="ViewModels.CertificateChainViewModel" />, whose instance is the panel's DataContext
///     (set by the host). The code-behind only initializes the component.
/// </summary>
public partial class CertificateChainPanel : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="CertificateChainPanel" /> class.</summary>
    public CertificateChainPanel()
    {
        InitializeComponent();
    }
}
