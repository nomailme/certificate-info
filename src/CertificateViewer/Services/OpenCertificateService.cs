using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using ShadUI;
using Window = Avalonia.Controls.Window;

namespace CertificateViewer.Services;

public class OpenCertificateService(DialogManager dialogManager, PasswordDialogViewModel passwordViewModel)
{
    private Lazy<Window?> MainWindow { get; set; } = new(GetMainWindow);

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        throw new NotSupportedException();
    }

    public async Task<DialogResult> OpenFile(string path)
    {
        var tcs = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var filepath = Uri.UnescapeDataString(path);
        var certificateType = await CertificateHelper.CheckAsync(filepath);
        if (certificateType == CertificateType.Pfx)
        {
            dialogManager.CreateDialog(passwordViewModel)

                .WithSuccessCallback(async vm =>
                {
                    var result = await LoadCertificate(filepath, CertificateType.Pfx, vm.Password);
                    tcs.SetResult(result);
                })
                .WithCancelCallback(() => tcs.SetResult(DialogResult.OperationCanceled()))
                .Show();

        }
        else
        {
            var result = await LoadCertificate(filepath, certificateType, string.Empty);
            tcs.SetResult(result);
        }

        return await tcs.Task;
    }

    public async Task<DialogResult> OpenFile2(string filename)
    {
        var dialogResult = DialogResult.OperationCanceled();
        var path = Uri.UnescapeDataString(filename);
        var certificateType = await CertificateHelper.CheckAsync(path);

        if (certificateType == CertificateType.Pfx)
        {
            dialogManager.CreateDialog(passwordViewModel)
                .WithSuccessCallback(vm =>
                {
                    dialogResult = LoadPfx(vm.Password).Result;
                })
                .WithCancelCallback(() => dialogResult = DialogResult.OperationCanceled())
                .Show();
        }
        else if  (certificateType == CertificateType.Unknown)
        {
            dialogResult = await LoadCertificate(path, certificateType);
        }

        return dialogResult;

        async Task<DialogResult>  LoadPfx(string secret)
        {
            var result = await LoadCertificate(path, certificateType, secret);
            return result;
        }
    }

    private static async Task<DialogResult> LoadCertificate(string filename, CertificateType certificateType, string password = "")
    {
        var rawData = await File.ReadAllBytesAsync(filename);
        var result = certificateType switch
        {
            CertificateType.Der => await new DerImporter().ImportAsync(rawData),
            CertificateType.Pem => await new PemImporter().ImportAsync(rawData),
            CertificateType.Pfx => await new PfxImporter().ImportAsync(rawData, new PfxImporter.PfxLoaderOptions { Password = password }),
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };
        return result.ToDialogResult(certificateType);
    }
}
