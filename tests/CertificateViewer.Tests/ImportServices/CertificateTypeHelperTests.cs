using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;

namespace CertificateViewer.Tests.ImportServices;

public class CertificateTypeHelperTests
{
    [Fact]
    public async Task Check_DerFile_DerFileType()
    {
        var result = await CertificateHelper.CheckAsync(Path.Combine(AppContext.BaseDirectory, "DataStore", "der.crt"));
        Assert.Equal(CertificateType.Der, result);
    }

    [Fact]
    public async Task Check_PemFile_PemFileType()
    {
        var result = await CertificateHelper.CheckAsync(Path.Combine(AppContext.BaseDirectory, "DataStore", "pem.crt"));
        Assert.Equal(CertificateType.Pem, result);

    }

    [Fact]
    public async Task Check_PfxFile_PfxFileType()
    {
        var result = await CertificateHelper.CheckAsync(Path.Combine(AppContext.BaseDirectory, "DataStore", "certificate.pfx"));
        Assert.Equal(CertificateType.Pfx, result);
    }
}