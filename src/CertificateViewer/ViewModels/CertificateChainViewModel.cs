using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateViewer.ViewModels;

/// <summary>
///     View model for the certificate-chain panel: holds the chain, its source description and
///     the currently selected certificate. The panel itself is a dumb view bound to this model;
///     its <see cref="Avalonia.Controls.UserControl.DataContext" /> is set by the host.
/// </summary>
public partial class CertificateChainViewModel : ObservableObject
{
    /// <summary>Gets or sets the collection of certificates in the chain.</summary>
    [ObservableProperty]
    public partial ObservableCollection<X509Certificate2> Items { get; set; } = new();

    /// <summary>Gets or sets a description of the source the certificate chain was loaded from.</summary>
    [ObservableProperty]
    public partial string CertificateSource { get; set; } = string.Empty;

    /// <summary>Gets or sets the currently selected certificate in the chain.</summary>
    [ObservableProperty]
    public partial X509Certificate2? SelectedCertificate { get; set; }
}
