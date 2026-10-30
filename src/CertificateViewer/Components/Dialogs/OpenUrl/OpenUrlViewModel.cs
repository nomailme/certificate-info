using System;
using CertificateViewer.ViewModels;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.Dialogs.OpenUrl;

/// <summary>
///     View model of the "open URL" dialog. Validation is explicit (no DataAnnotations, trim-safe):
///     the address must be present and have a valid host name (name or IP literal).
/// </summary>
public sealed partial class OpenUrlViewModel(ShadUI.DialogManager dialogManager) : DialogViewModelBase(dialogManager)
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

        // CheckHostName takes a *host name* (e.g. "example.com"), not a full URL: passing the raw
        // input rejects every "scheme://host" address, so the host must be parsed first. A failed
        // TryCreate yields a null uri and an empty host, both of which mean "no valid address".
        if (Uri.TryCreate(Url, UriKind.Absolute, out var uri) == false || uri.Host.Length == 0)
        {
            AddError(UrlPropertyName, "Provided address is not a valid address");
        }
    }

    // Closing must be keyed on the concrete type (OpenUrlViewModel) so ShadUI matches the callback the
    // app registered via CreateDialog(vm); see DialogViewModelBase for the full rationale.
    protected override void CloseWithSuccess()
    {
        DialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    protected override void CloseAsCancelled() => DialogManager.Close(this);
}
