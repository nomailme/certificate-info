using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;

namespace CertificateViewer.Tests.ImportServices;

public class DerDetectionTests
{
    [Fact]
    public async Task ExistingDerFixture_IsDetectedAsDer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "DataStore", "der.crt");
        Assert.True(File.Exists(path));

        var result = await CertificateHelper.IsDerCertificate(path);

        Assert.True(result);
    }

    [Fact]
    public async Task ShortFormUnder256Bytes_IsDetectedAsDer()
    {
        // SEQUENCE with short-form length 0x61 (97 bytes) => 99 content bytes,
        // 100 bytes total. The old {0x30, 0x82} prefix check missed this.
        var buffer = new byte[100];
        buffer[0] = 0x30;
        buffer[1] = 0x61;

        var result = await CertificateHelper.IsDerCertificate(buffer);

        Assert.True(result);
    }

    [Fact]
    public async Task LongFormOneByteLength_IsDetectedAsDer()
    {
        // SEQUENCE with long-form length (0x81) and one length byte 0x40.
        var buffer = new byte[0x42];
        buffer[0] = 0x30;
        buffer[1] = 0x81;
        buffer[2] = 0x40;

        var result = await CertificateHelper.IsDerCertificate(buffer);

        Assert.True(result);
    }

    [Fact]
    public async Task IndefiniteLength_IsNotDetectedAsDer()
    {
        var buffer = new byte[0x42];
        buffer[0] = 0x30;
        buffer[1] = 0x80;

        var result = await CertificateHelper.IsDerCertificate(buffer);

        Assert.False(result);
    }

    [Fact]
    public async Task PemHeader_IsNotDetectedAsDer()
    {
        var pemHeader = "-----BEGIN CERTIFICATE-----"u8.ToArray();

        var result = await CertificateHelper.IsDerCertificate(pemHeader);

        Assert.False(result);
    }
}
