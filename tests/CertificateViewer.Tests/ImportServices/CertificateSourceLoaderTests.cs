using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Tests for the URL-load service (2.14): the import/error-mapping logic lives outside the
///     UI, so it is exercised here with fakes and no UI.
/// </summary>
public class CertificateSourceLoaderTests
{
    private static X509Certificate2 SelfSigned()
    {
        // A real certificate is needed for a successful result; the content is irrelevant here.
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
    }

    private sealed class FakeImporter : ICertificateLoader<string>
    {
        private readonly ImportResult _result;

        public FakeImporter(ImportResult result) => _result = result;

        public Task<ImportResult> ImportAsync(string input, EmptyOptions? options = default) => Task.FromResult(_result);
    }

    private sealed class RecordingErrorHandler : IErrorHandler
    {
        public readonly List<string> Contexts = new();

        public Task HandleAsync(Exception exception, string? context = null)
        {
            Contexts.Add(context ?? string.Empty);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadAsync_WithEmptyUri_ReturnsFailureWithoutImportOrError()
    {
        var importer = new FakeImporter(ImportResult.CreateSuccess(new List<X509Certificate2> { SelfSigned() }));
        var errorHandler = new RecordingErrorHandler();
        var loader = new CertificateSourceLoader(importer, errorHandler);

        var result = await loader.LoadAsync("   ");

        Assert.False(result.Success);
        Assert.Null(result.Certificates);
        Assert.Empty(errorHandler.Contexts);
    }

    [Fact]
    public async Task LoadAsync_WithImporterFailure_ReturnsFailureAndReportsToErrorHandler()
    {
        var importer = new FakeImporter(ImportResult.CreateFail(new InvalidOperationException("no chain")));
        var errorHandler = new RecordingErrorHandler();
        var loader = new CertificateSourceLoader(importer, errorHandler);

        var result = await loader.LoadAsync("https://example.com/");

        Assert.False(result.Success);
        Assert.Null(result.Certificates);
        Assert.Equal(new[] { "Loading URL" }, errorHandler.Contexts);
    }

    [Fact]
    public async Task LoadAsync_WithSuccess_ReturnsCertificatesSourceAndTitle()
    {
        var importer = new FakeImporter(ImportResult.CreateSuccess(new List<X509Certificate2> { SelfSigned() }));
        var loader = new CertificateSourceLoader(importer, new RecordingErrorHandler());

        var result = await loader.LoadAsync("https://sub.example.com:8443/path");

        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.Single(result.Certificates!);
        Assert.Equal("url:sub.example.com", result.Source);
        Assert.Equal("https://sub.example.com:8443/path", result.Title);
    }

    [Fact]
    public async Task LoadAsync_WithImporterFailureAndNoError_ReturnsFailureAndReportsToErrorHandler()
    {
        var importer = new FakeImporter(ImportResult.CreateFail(null!));
        var errorHandler = new RecordingErrorHandler();
        var loader = new CertificateSourceLoader(importer, errorHandler);

        var result = await loader.LoadAsync("https://example.com/");

        Assert.False(result.Success);
        Assert.Single(errorHandler.Contexts);
    }
}

/// <summary>
///     Regression: an endpoint that serves no certificates (e.g. a plain http:// address) must be
///     reported as a failure with an error surfaced, not as a success with an empty chain (which
///     would leave the UI blank).
/// </summary>
public class CertificateSourceLoaderNoCertTests
{
    private sealed class NoCertImporter : ICertificateLoader<string>
    {
        public Task<ImportResult> ImportAsync(string input, EmptyOptions? options = default) =>
            Task.FromResult(ImportResult.CreateSuccess(new List<X509Certificate2>()));
    }

    private sealed class RecordingErrorHandler : IErrorHandler
    {
        public readonly List<string> Contexts = new();
        public Exception? Last;

        public Task HandleAsync(Exception exception, string? context = null)
        {
            Last = exception;
            Contexts.Add(context ?? string.Empty);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadAsync_WithNoCertificates_ReturnsFailureAndReportsToErrorHandler()
    {
        var importer = new NoCertImporter();
        var errorHandler = new RecordingErrorHandler();
        var loader = new CertificateSourceLoader(importer, errorHandler);

        var result = await loader.LoadAsync("http://example.com");

        Assert.False(result.Success);
        Assert.Null(result.Certificates);
        Assert.NotNull(errorHandler.Last);
        Assert.Equal(new[] { "Loading URL" }, errorHandler.Contexts);
    }
}
