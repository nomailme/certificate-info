using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class PemImporterTests
{
    private readonly ICertificateLoader<byte[]> _importer = new PemImporter();
    
    [Fact]
    public async Task Import_PemFile_Success()
    {
     
        
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\pem.crt"));
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Single(result.Certificates);
    } 
    
    [Fact]
    public async Task Import_PemChainFile_Success()
    {
     
        
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\pem_chain.crt"));
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Equal(2,result.Certificates.Count);
    }

    [Fact]
    public async Task Import_PfxFile_Failure()
    {
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\certificate.pfx"));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Import_DerFile_Failure()
    {
        var result = await _importer.ImportAsync(await File.ReadAllBytesAsync(@"DataStore\der.crt"));
        Assert.False(result.Success);
    }
}