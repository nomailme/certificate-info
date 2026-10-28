using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services.ErrorHandling;
namespace CertificateViewer.Services;

/// <summary>
///     Result of a URL certificate load attempt. <see cref="Certificates" /> is null when the load
///     produced no certificates (empty input or a failure).
/// </summary>
/// <param name="Success">Whether the load produced certificates.</param>
/// <param name="Certificates">The loaded certificates (empty input or a failure yields null).</param>
/// <param name="Source">The source description (e.g. "url:example.com").</param>
/// <param name="Title">The window title contribution (the original URI).</param>
public sealed record UrlLoadResult(bool Success, IReadOnlyList<X509Certificate2>? Certificates, string Source, string Title);

/// <summary>
///     Loads a certificate chain from a URL: URI validation, the remote-server import and the
///     error mapping. Deliberately UI-free so it can be unit-tested without the view layer; the
///     window view model only applies the result (busy indicator, chain, title).
/// </summary>
public class CertificateSourceLoader(ICertificateLoader<string> remoteServerImporter, IErrorHandler errorHandler)
{
    /// <summary>
    ///     Loads the certificate chain for the given URI. Failures (including an empty URI)
    ///     surface through the injected <see cref="IErrorHandler" />; the method never throws for
    ///     them and returns a failed <see cref="UrlLoadResult" />.
    /// </summary>
    /// <param name="uri">The URI to load.</param>
    /// <returns>The load result: success with certificates, or failure with no certificates.</returns>
    public async Task<UrlLoadResult> LoadAsync(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return new UrlLoadResult(false, null, string.Empty, string.Empty);
        }

        try
        {
            var result = await remoteServerImporter.ImportAsync(uri);
            if (!result.Success)
            {
                throw result.Error ?? new InvalidOperationException("Error loading certificate");
            }

            return new UrlLoadResult(
                true,
                result.Certificates is null ? new List<X509Certificate2>() : new List<X509Certificate2>(result.Certificates),
                $"url:{new Uri(uri).Host}",
                uri);
        }
        catch (Exception e)
        {
            await errorHandler.HandleAsync(e, context: "Loading URL");
            return new UrlLoadResult(false, null, string.Empty, uri);
        }
    }
}
