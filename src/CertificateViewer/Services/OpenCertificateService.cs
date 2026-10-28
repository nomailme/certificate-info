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

/// <summary>
///     Loads certificates from files. The importers are provided via the constructor (DI), so
///     no importer is constructed at call sites.
/// </summary>
public class OpenCertificateService(
    DialogManager dialogManager,
    PasswordDialogViewModel passwordViewModel,
    DerImporter derImporter,
    PemImporter pemImporter,
    PfxImporter pfxImporter)
{
    private const string UnsupportedFileTypeMessage = "Unrecognized or unsupported file type.";

    public async Task<DialogResult> OpenFile(string path)
    {
        var filepath = Uri.UnescapeDataString(path);
        // Read the file exactly once and detect the type from the same bytes.
        var rawData = await File.ReadAllBytesAsync(filepath);
        var certificateType = await CertificateHelper.CheckAsync(rawData);

        if (certificateType == CertificateType.Pfx)
        {
            // The password dialog is fire-and-forget: Show() returns void and the
            // success callback fires later, so it cannot be awaited directly.
            // The TCS bridges that callback into the awaited Task<DialogResult>.
            var tcs = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            dialogManager.CreateDialog(passwordViewModel)
                .WithSuccessCallback(async vm =>
                {
                    tcs.SetResult(await TryLoadCertificateAsync(rawData, CertificateType.Pfx, vm.Password));
                })
                .WithCancelCallback(() => tcs.SetResult(DialogResult.OperationCanceled()))
                .Show();
            return await tcs.Task;
        }

        return await TryLoadCertificateAsync(rawData, certificateType, string.Empty);
    }

    private async Task<DialogResult> TryLoadCertificateAsync(byte[] rawData, CertificateType certificateType, string password)
    {
        try
        {
            if (certificateType == CertificateType.Unknown)
            {
                return DialogResult.CreateFail(new InvalidOperationException(UnsupportedFileTypeMessage));
            }
            return await LoadCertificate(rawData, certificateType, password);
        }
        catch (ArgumentException)
        {
            return DialogResult.CreateFail(new InvalidOperationException(UnsupportedFileTypeMessage));
        }
    }

    private async Task<DialogResult> LoadCertificate(byte[] rawData, CertificateType certificateType, string password = "")
    {
        var result = certificateType switch
        {
            CertificateType.Der => await derImporter.ImportAsync(rawData),
            CertificateType.Pem => await pemImporter.ImportAsync(rawData),
            CertificateType.Pfx => await pfxImporter.ImportAsync(rawData, new PfxImporter.PfxLoaderOptions { Password = password }),
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };
        return result.ToDialogResult(certificateType);
    }
}
