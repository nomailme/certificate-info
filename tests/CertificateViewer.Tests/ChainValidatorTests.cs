using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Tests for <see cref="ChainValidator" /> (previously untested). A leaf signed by a CA
///     validates when that CA is supplied as a custom root; the same chain is reported untrusted
///     when the CA is only present in the extra store (intermediates) but not the trust store,
///     since the extra store does not act as a trust anchor. An unanchored single cert also fails
///     with status information.
/// </summary>
public class ChainValidatorTests
{
    private readonly ChainValidator _validator = new();

    [Fact]
    public async Task ValidChain_CustomRoot_NoErrors()
    {
        var (leaf, ca) = TestCertificates.Chain();
        var chain = new List<X509Certificate2> { leaf, ca };

        var errors = await _validator.Validate(chain, new List<X509Certificate2> { ca }, useSystemStore: false);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task CaOnlyInExtraStore_NoTrustAnchor_ReportsUntrustedStatus()
    {
        // The validator places every non-leaf cert in the extra store, but the extra store only
        // supplies intermediates — it is not a trust anchor. With no custom root, a chain to a
        // self-signed CA must be reported untrusted (not validated as if the extra store trusted it).
        var (leaf, ca) = TestCertificates.Chain();
        var chain = new List<X509Certificate2> { leaf, ca };

        var errors = await _validator.Validate(chain, new List<X509Certificate2>(), useSystemStore: false);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task EmptyChain_ReturnsNoCertificateError()
    {
        var errors = await _validator.Validate(
            new List<X509Certificate2>(), new List<X509Certificate2>(), useSystemStore: false);

        Assert.Single(errors);
        Assert.Contains("No certificate to validate.", errors[0]);
    }

    [Fact]
    public async Task SelfSignedLeaf_NoTrustAnchor_ReportsUntrustedStatus()
    {
        // A self-signed cert with an empty custom store cannot be trusted, so the chain status
        // must surface at least one problem (not an empty "valid" result).
        var leaf = TestCertificates.SelfSigned("CN=Untrusted Leaf");
        var chain = new List<X509Certificate2> { leaf };

        var errors = await _validator.Validate(chain, new List<X509Certificate2>(), useSystemStore: false);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task ChainSignedByUnknownCa_NoTrustAnchor_ReportsUntrustedStatus()
    {
        var (leaf, _) = TestCertificates.Chain();
        var chain = new List<X509Certificate2> { leaf };

        var errors = await _validator.Validate(chain, new List<X509Certificate2>(), useSystemStore: false);

        Assert.NotEmpty(errors);
    }
}
