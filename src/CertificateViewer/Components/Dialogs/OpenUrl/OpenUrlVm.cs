using System;
using CertificateViewer.ViewModels;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.Dialogs.OpenUrl;

/// <summary>
///     View model of the "open URL" dialog. Validation is explicit (no DataAnnotations, trim-safe):
///     the address must be present and have a valid host name (name or IP literal).
/// </summary>
public sealed partial class OpenUrlVm(ShadUI.DialogManager dialogManager) : DialogViewModelBase(dialogManager)
{
    private const string UrlPropertyName = nameof(Url);

    private string _url = "https://";

    /// <summary>Gets or sets the address the certificate chain is loaded from.</summary>
    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    /// <summary>Validates the address (explicit, per-property).</summary>
    protected override void ValidateAll()
    {
        ClearErrors(UrlPropertyName);
        if (string.IsNullOrWhiteSpace(Url))
        {
            AddError(UrlPropertyName, "Provide a base url");
            return;
        }

        var hostType = Uri.CheckHostName(Url);
        if (hostType == UriHostNameType.Unknown)
        {
            AddError(UrlPropertyName, "Provided address is not a valid address");
        }
    }
}
