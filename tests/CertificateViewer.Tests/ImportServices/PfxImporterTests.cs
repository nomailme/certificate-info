using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class PfxImporterTests
{
    private readonly PfxImporter _importer = new();
    
    [Fact]
    public async Task Import_PfxFile_Success()
    {
        var options = new PfxImporter.PfxLoaderOptions
        { 
            Password = "123123123"
        };
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\certificate.pfx"),  options);
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Single(result.Certificates);
    }

    [Fact]
    public async Task Import_PfxFileWithWrongPassword_Fail()
    {
        var options = new PfxImporter.PfxLoaderOptions();
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\certificate.pfx"), options);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_PemFile_Failure()
    {
        var options = new PfxImporter.PfxLoaderOptions();
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\pem.crt"), options);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_DerFile_Failure()
    {
        var options = new PfxImporter.PfxLoaderOptions();
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\der.crt"), options);
        Assert.False(result.Success);
    }
}