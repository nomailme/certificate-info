using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CertificateViewer.Logic;

/// <summary>
///     Validates an X.509 certificate chain against a trust store (system or custom).
/// </summary>
public interface ICertificateChainValidator
{
    /// <summary>
    ///     Validates the chain. Returns a list of error messages; an empty list means the chain is valid.
    /// </summary>
    /// <param name="certificates">The full chain, leaf first.</param>
    /// <param name="rootCertificates">Custom roots to use when <paramref name="useSystemStore" /> is false.</param>
    /// <param name="useSystemStore">When true, use the system trust store; otherwise use the provided roots.</param>
    Task<List<string>> Validate(ICollection<X509Certificate2> certificates, ICollection<X509Certificate2>? rootCertificates, bool useSystemStore);
}
