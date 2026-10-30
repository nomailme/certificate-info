namespace CertificateViewer.Logic.ImportServices;

/// <summary>
///     A certificate importer that works on raw file bytes. The password parameter is used by
///     importers that need it (e.g. PFX) and ignored by others (e.g. DER, PEM). Selection is
///     strategy-based: the first registered importer whose <see cref="IsSupported" /> returns
///     true for the input bytes is used, replacing the former switch in the service.
/// </summary>
public interface ICertificateFileImporter
{
    /// <summary>Whether this importer can handle the given raw bytes.</summary>
    bool IsSupported(byte[] data);

    /// <summary>Imports certificates from the given bytes.</summary>
    Task<ImportResult> ImportAsync(byte[] data, string password);
}
