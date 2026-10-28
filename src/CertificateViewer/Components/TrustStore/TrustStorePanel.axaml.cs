using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace CertificateViewer.Components.TrustStore;

/// <summary>
///     Dumb view for the trust store: all state and commands live on
///     <see cref="ViewModels.TrustStoreViewModel" />, whose instance is the panel's DataContext
///     (set by the host). The code-behind only initializes the component and shows the warning
///     flyout when the pointer enters the info icon.
/// </summary>
public partial class TrustStorePanel : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="TrustStorePanel" /> class.</summary>
    public TrustStorePanel()
    {
        InitializeComponent();
    }

    /// <summary>Shows the attached warning flyout when the pointer enters the info icon.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The pointer event args.</param>
    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e) =>
        FlyoutBase.ShowAttachedFlyout(BorderWarning);
}
