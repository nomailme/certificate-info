using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class PemImporterTests
{
    private readonly PemImporter _importer = new();
    
    [Fact]
    public async Task Import_PemFile_Success()
    {
        
        var result = await _importer.ImportAsync(@"DataStore\pem.crt");
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Single(result.Certificates);
    }

    [Fact]
    public async Task Import_PfxFile_Failure()
    {
        var result = await _importer.ImportAsync(@"DataStore\certificate.pfx");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_DerFile_Failure()
    {
        var result = await _importer.ImportAsync(@"DataStore\der.crt");
        Assert.False(result.Success);
    }
}