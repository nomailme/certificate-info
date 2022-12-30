using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class PfxImporterTests
{
    private readonly PfxImporter _importer = new();
    
    [Fact]
    public async Task Import_PfxFile_Success()
    {
        var result = await _importer.ImportAsync(@"DataStore\certificate.pfx", "123123123");
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Single(result.Certificates);
    }

    [Fact]
    public async Task Import_PfxFileWithWrongPassword_Fail()
    {
        var result = await _importer.ImportAsync(@"DataStore\certificate.pfx", string.Empty);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_PemFile_Failure()
    {
        var result = await _importer.ImportAsync(@"DataStore\pem.crt", string.Empty);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_DerFile_Failure()
    {
        var result = await _importer.ImportAsync(@"DataStore\der.crt", string.Empty);
        Assert.False(result.Success);
    }
}