using System.ComponentModel.DataAnnotations;
using CertificateViewer.ViewModels;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.Dialogs.PasswordBox;

public partial class PasswordDialogViewModel(ShadUI.DialogManager dialogManager) : BaseViewModel
{
    private string _password = string.Empty;

    [Required(ErrorMessage = "Provide a password")]
    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }
    private bool CanSubmit() => !HasErrors;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        ClearAllErrors();
        ValidateAllProperties();

        if (HasErrors)
        {
            return;
        }

        dialogManager.Close(this,
            new CloseDialogOptions
            {
                Success = true
            });
    }

    [RelayCommand]
    private void Cancel() => dialogManager.Close(this);
}
