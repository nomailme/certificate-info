using System;
using System.Threading.Tasks;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Logic;
using ShadUI;

namespace CertificateViewer.Services;

/// <summary>
///     UI implementation of <see cref="IPasswordPrompt" />: shows the password dialog through
///     the <see cref="DialogManager" /> and bridges its fire-and-forget success/cancel callbacks
///     into an awaited result (null when the user cancels). Keeps the dialog machinery out of the
///     domain service so <c>OpenCertificateService</c> stays UI-free and unit-testable.
/// </summary>
public class PasswordPrompt(DialogManager dialogManager, PasswordDialogViewModel passwordViewModel) : IPasswordPrompt
{
    public async Task<string?> PromptAsync()
    {
        // The password dialog is fire-and-forget: Show() returns void and the success callback
        // fires later, so it cannot be awaited directly. The TCS bridges that callback into the
        // awaited Task<string?>.
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialogManager.CreateDialog(passwordViewModel)
            .WithSuccessCallback(async vm => tcs.SetResult(await Task.FromResult(vm.Password)))
            .WithCancelCallback(() => tcs.SetResult(null))
            .Show();
        return await tcs.Task;
    }
}
