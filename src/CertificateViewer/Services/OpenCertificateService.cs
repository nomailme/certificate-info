using System;
using System.IO;
using System.Threading.Tasks;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using ShadUI;

namespace CertificateViewer.Services;

public class OpenCertificateService(DialogManager dialogManager, PasswordDialogViewModel passwordViewModel)
{
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
