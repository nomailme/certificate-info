namespace CertificateViewer.Logic;

/// <summary>
///     Abstraction over the file-opening service so view models can be unit-tested with a fake
///     (Phase 3: VM-level test strategy) instead of the real, file-reading implementation. The
///     concrete <see cref="OpenCertificateService" /> performs the actual import.
/// </summary>
public interface IOpenCertificateService
{
    /// <summary>
    ///     Loads the certificates from the file at <paramref name="path" /> and returns the result.
    ///     Cancellation (e.g. the user dismissing a password prompt) is reported as a canceled
    ///     <see cref="DialogResult" /> rather than an exception.
    /// </summary>
    /// <param name="path">The file path to open.</param>
    /// <returns>The dialog result describing the loaded certificates or the failure.</returns>
    Task<DialogResult> OpenFile(string path);
}
