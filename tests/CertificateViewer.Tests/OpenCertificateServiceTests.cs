using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Tests for the domain certificate-loading service after the Phase 3 boundary move. The
///     service is UI-free: the password prompt is an injected <see cref="IPasswordPrompt" /> and
///     importer selection is strategy-based (first registered importer that supports the bytes),
///     so both are exercised with fakes and real importer instances, with no view layer involved.
/// </summary>
public class OpenCertificateServiceTests
{
    private sealed class FakePasswordPrompt(string? result) : IPasswordPrompt
    {
        public int Calls { get; set; }

        public Task<string?> PromptAsync()
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private static OpenCertificateService Service(FakePasswordPrompt prompt,
        params ICertificateFileImporter[] importers)
        => new(prompt, importers);

    private static byte[] PfxBytes(string password)
    {
        return TestCertificates.SelfSigned()
            .Export(X509ContentType.Pfx, password);
    }

    [Fact]
    public async Task OpenFile_PemFile_LoadsCertificates_WithoutPrompt()
    {
        var prompt = new FakePasswordPrompt("should-not-be-asked");
        var service = Service(prompt, new PemImporter(), new DerImporter(), new PfxImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.pem");
        await File.WriteAllBytesAsync(path, TestCertificates.ToPem(TestCertificates.SelfSigned()));

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Success, result.Success);
            Assert.Single(result.Certificates);
            Assert.Equal(CertificateType.Pem, result.Type);
            Assert.Equal(0, prompt.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenFile_DerFile_LoadsCertificates_WithoutPrompt()
    {
        var prompt = new FakePasswordPrompt("should-not-be-asked");
        var service = Service(prompt, new DerImporter(), new PemImporter(), new PfxImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.der");
        await File.WriteAllBytesAsync(path, TestCertificates.SelfSigned().RawData);

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Success, result.Success);
            Assert.Single(result.Certificates);
            Assert.Equal(CertificateType.Der, result.Type);
            Assert.Equal(0, prompt.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenFile_PfxFile_PromptsAndLoadsWithPassword()
    {
        const string password = "testpass123";
        var prompt = new FakePasswordPrompt(password);
        var service = Service(prompt, new PfxImporter(), new PemImporter(), new DerImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(path, PfxBytes(password));

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Success, result.Success);
            Assert.Single(result.Certificates);
            Assert.Equal(CertificateType.Pfx, result.Type);
            Assert.Equal(1, prompt.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenFile_PfxFile_WhenPromptCanceled_ReturnsCanceled()
    {
        var prompt = new FakePasswordPrompt(null);
        var service = Service(prompt, new PfxImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(path, PfxBytes("unused"));

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Canceled, result.Success);
            Assert.Equal(1, prompt.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenFile_PfxFile_WrongPassword_ReturnsFailure()
    {
        var prompt = new FakePasswordPrompt("wrong-password");
        var service = Service(prompt, new PfxImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(path, PfxBytes("testpass123"));

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Failure, result.Success);
            Assert.NotNull(result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenFile_UnknownType_ReturnsFailure_WithoutPrompt()
    {
        var prompt = new FakePasswordPrompt("should-not-be-asked");
        var service = Service(prompt, new PemImporter(), new DerImporter(), new PfxImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(path, new byte[] { 0x01, 0x02, 0x03, 0x04 });

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Failure, result.Success);
            Assert.NotNull(result.Error);
            Assert.Contains("Unrecognized or unsupported file type.", result.Error.Message);
            Assert.Equal(0, prompt.Calls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Selection_StrategyBased_NoImporterMatches_Throws()
    {
        // An importer that claims nothing: only reachable if type detection and importer
        // claims disagree; the service must fail loudly, not guess.
        var prompt = new FakePasswordPrompt(null);
        var service = Service(prompt, new NothingImporter());
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.der");
        await File.WriteAllBytesAsync(path, TestCertificates.SelfSigned().RawData);

        try
        {
            var result = await service.OpenFile(path);
            Assert.Equal(DialogResult.OperationResult.Failure, result.Success);
            Assert.Contains("Unrecognized or unsupported file type.", result.Error!.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class NothingImporter : ICertificateFileImporter
    {
        public bool IsSupported(byte[] data) => false;

        public Task<ImportResult> ImportAsync(byte[] data, string password)
            => throw new NotSupportedException();
    }
}
