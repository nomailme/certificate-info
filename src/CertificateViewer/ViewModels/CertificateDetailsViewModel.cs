using System;
using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateViewer.ViewModels;

/// <summary>
///     View model for the certificate details panel: holds the displayed certificate and the
///     view-state (General/Raw/Base64). The panel is a dumb view whose DataContext is this VM
///     (set by the host); leaf bindings to the certificate's model properties bind through
///     <see cref="Certificate" />.
/// </summary>
public partial class CertificateDetailsViewModel : ObservableObject
{
    /// <summary>Gets the available detail view modes.</summary>
    public ViewMode[] ViewModes { get; } = Enum.GetValues<ViewMode>();

    /// <summary>Gets or sets the certificate being displayed.</summary>
    [ObservableProperty]
    public partial X509Certificate2? Certificate { get; set; }

    /// <summary>Gets or sets the active view mode (General, Raw, or Base64).</summary>
    [ObservableProperty]
    public partial ViewMode ViewMode { get; set; } = ViewMode.General;
}
