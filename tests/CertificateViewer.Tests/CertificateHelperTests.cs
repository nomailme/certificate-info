using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Tests for <see cref="CertificateHelper" /> (previously untested): type detection over raw
///     bytes and PEM/DER structure checks, complementing the existing fixture-based suites.
/// </summary>
public class CertificateHelperTests
{
    private static byte[] PfxBytes() =>
        TestCertificates.SelfSigned().Export(X509ContentType.Pfx, "x");

    [Fact]
    public async Task CheckRaw_PemBytes_ReturnsPem()
    {
        var result = await CertificateHelper.CheckAsync(TestCertificates.ToPem(TestCertificates.SelfSigned()));
        Assert.Equal(CertificateType.Pem, result);
    }

    [Fact]
    public async Task CheckRaw_DerBytes_ReturnsDer()
    {
        var result = await CertificateHelper.CheckAsync(TestCertificates.SelfSigned().RawData);
        Assert.Equal(CertificateType.Der, result);
    }

    [Fact]
    public async Task CheckRaw_PfxBytes_ReturnsPfx()
    {
        var result = await CertificateHelper.CheckAsync(PfxBytes());
        Assert.Equal(CertificateType.Pfx, result);
    }

    [Fact]
    public async Task CheckRaw_Garbage_ReturnsUnknown()
    {
        var result = await CertificateHelper.CheckAsync(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        Assert.Equal(CertificateType.Unknown, result);
    }

    [Fact]
    public void IsPemPemBytes_True()
    {
        Assert.True(CertificateHelper.IsPemCertificate(TestCertificates.ToPem(TestCertificates.SelfSigned())));
    }

    [Fact]
    public void IsPemDerBytes_False()
    {
        Assert.False(CertificateHelper.IsPemCertificate(TestCertificates.SelfSigned().RawData));
    }

    [Fact]
    public async Task IsDer_DerBytes_True()
    {
        Assert.True(await CertificateHelper.IsDerCertificate(TestCertificates.SelfSigned().RawData));
    }

    [Fact]
    public async Task IsDer_PemBytes_False()
    {
        Assert.False(await CertificateHelper.IsDerCertificate(TestCertificates.ToPem(TestCertificates.SelfSigned())));
    }

    [Fact]
    public async Task IsDer_EmptyBytes_False()
    {
        Assert.False(await CertificateHelper.IsDerCertificate(Array.Empty<byte>()));
    }

    [Fact]
    public async Task IsDer_SingleByte_False()
    {
        Assert.False(await CertificateHelper.IsDerCertificate(new byte[] { 0x30 }));
    }
}
