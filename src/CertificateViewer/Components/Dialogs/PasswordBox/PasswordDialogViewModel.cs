using CertificateViewer.ViewModels;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.Dialogs.PasswordBox;

/// <summary>
///     View model of the "password" dialog. Validation is explicit (no DataAnnotations,
///     trim-safe): a password must be present.
/// </summary>
public partial class PasswordDialogViewModel(ShadUI.DialogManager dialogManager) : DialogViewModelBase(dialogManager)
{
    private const string PasswordPropertyName = nameof(Password);

    private string _password = string.Empty;

    /// <summary>Gets or sets the password.</summary>
    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    /// <summary>Validates the password (explicit, per-property).</summary>
    protected override void ValidateAll()
    {
        ClearErrors(PasswordPropertyName);
        if (string.IsNullOrWhiteSpace(Password))
        {
            AddError(PasswordPropertyName, "Provide a password");
        }
    }

    // Closing must be keyed on the concrete type so ShadUI matches the callback the app
    // registered via CreateDialog(vm); see DialogViewModelBase for the full rationale.
    protected override void CloseWithSuccess()
    {
        DialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    protected override void CloseAsCancelled() => DialogManager.Close(this);
}
