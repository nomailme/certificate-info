using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Services;

public class OpenCertificateService
{
    private Lazy<Window> MainWindow { get; set; } = new(GetMainWindow);

    private static Window GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        throw new NotSupportedException();
    }

    public async Task<DialogResult> OpenFile(string filename)
    {
        try
        {
            var password = string.Empty;
            var certificateType = await CertificateHelper.CheckAsync(filename);

            if (certificateType == CertificateType.Pfx)
            {
                password = await PasswordBox.ShowPasswordBoxAsync(MainWindow.Value);
                if (password == null)
                {
                    return DialogResult.OperationCanceled();
                }
            }

            var result = await LoadCertificate(filename, certificateType, password);
            return result;
        }
        catch (Exception ex)
        {
            return DialogResult.CreateFail(ex);
        }
    }

    private static async Task<DialogResult> LoadCertificate(string filename, CertificateType certificateType, string password = "")
    {
        var result = certificateType switch
        {
            CertificateType.Der => await new DerImporter().ImportAsync(filename),
            CertificateType.Pem => await new PemImporter().ImportAsync(filename),
            CertificateType.Pfx => await new PfxImporter().ImportAsync(filename, password),
            _ => throw new ArgumentOutOfRangeException(nameof(certificateType), certificateType, null)
        };
        return result.ToDialogResult(certificateType);
    }
}
