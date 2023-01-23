using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class RemoteServerImporterTests
{
    [Fact]
    public async Task Check()
    {
        var testAddress = "https://google.com";
        var importer = new RemoteServerImporter();
        var result = await importer.ImportAsync(testAddress);
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.NotEmpty(result.Certificates);
    }
}