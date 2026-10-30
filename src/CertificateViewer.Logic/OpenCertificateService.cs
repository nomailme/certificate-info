using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic.ImportServices;

namespace CertificateViewer.Logic;

/// <summary>
///     Loads certificates from files. The importers are provided via the constructor (DI), so
///     no importer is constructed at call sites. Importer selection is strategy-based: the first
///     registered importer whose <see cref="ICertificateFileImporter.IsSupported" /> returns true
///     for the input bytes is used, rather than a switch on the detected type.
/// </summary>
public class OpenCertificateService : IOpenCertificateService
{
    private readonly IPasswordPrompt passwordPrompt;
    // IEnumerable, not IReadOnlyList: MS.DI resolves open-generic collection parameters for
    // IEnumerable<T> (and a handful of other collection types) from multiple T registrations,
    // but not IReadOnlyList<T> — constructing against IReadOnlyList crashed the app at startup
    // with "Unable to resolve service for type IReadOnlyList<ICertificateFileImporter>".
    private readonly IEnumerable<ICertificateFileImporter> fileImporters;

    public OpenCertificateService(
        IPasswordPrompt passwordPrompt,
        IEnumerable<ICertificateFileImporter> fileImporters)
    {
        this.passwordPrompt = passwordPrompt;
        this.fileImporters = fileImporters;
    }

    private const string UnsupportedFileTypeMessage = "Unrecognized or unsupported file type.";

    public async Task<DialogResult> OpenFile(string path)
    {
        var filepath = Uri.UnescapeDataString(path);
        // Read the file exactly once and detect the type from the same bytes.
        var rawData = await File.ReadAllBytesAsync(filepath);
        var certificateType = await CertificateHelper.CheckAsync(rawData);

        if (certificateType == CertificateType.Pfx)
        {
            // Prompt for the password through the UI seam; a null result means the user
            // cancelled the dialog, which is surfaced as an operation-canceled result.
            var password = await passwordPrompt.PromptAsync();
            if (password is null)
            {
                return DialogResult.OperationCanceled();
            }
            return await TryLoadCertificateAsync(rawData, CertificateType.Pfx, password);
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
        catch (InvalidOperationException ex) when (ex.Message == UnsupportedFileTypeMessage)
        {
            // Type detection and the importers' IsSupported claims disagreed, so no importer
            // claimed the input. Fail loudly with a friendly result rather than guessing.
            return DialogResult.CreateFail(ex);
        }
    }

    private async Task<DialogResult> LoadCertificate(byte[] rawData, CertificateType certificateType, string password)
    {
        // Strategy-based selection: the first importer that claims the input is used.
        // File-type importers are mutually exclusive (PFX, PEM, DER), so the match is unique.
        var importer = fileImporters.FirstOrDefault(i => i.IsSupported(rawData))
            ?? throw new InvalidOperationException(UnsupportedFileTypeMessage);
        var result = await importer.ImportAsync(rawData, password);
        return result.ToDialogResult(certificateType);
    }
}
