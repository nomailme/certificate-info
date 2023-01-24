using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class RemoteServerImporterTests
{
    [Fact]
    public async Task Check()
    {
        string testAddress = "https://google.com";
        ICertificateLoader<string> importer = new RemoteServerImporter();
        ImportResult result = await importer.ImportAsync(testAddress);
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.NotEmpty(result.Certificates);
    }
}