using System.ComponentModel.DataAnnotations;
using CertificateViewer.Validators;
using CertificateViewer.ViewModels;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.Dialogs.OpenUrl;

public sealed partial class OpenUrlViewModel2(ShadUI.DialogManager dialogManager) : BaseViewModel
{
    private string _url = "https://";

    [Required(ErrorMessage = "Provide a base url")]
    [DomainNameValidation]
    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
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
