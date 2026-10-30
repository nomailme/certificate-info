using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using CertificateViewer.ViewModels;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Tests for the trust-store view model (Phase 3: VM-level test strategy). The file service,
///     chain validator and error handler are all fakes, so no UI and no network are involved;
///     the file-open path is exercised through real temp files.
/// </summary>
public class TrustStoreViewModelTests : IDisposable
{
    private sealed class FakeService : IOpenCertificateService
    {
        public DialogResult Result { get; set; } = DialogResult.OperationCanceled();

        public Task<DialogResult> OpenFile(string path) => Task.FromResult(Result);
    }

    private sealed class FakeValidator : ICertificateChainValidator
    {
        public List<string> Result { get; set; } = new();

        public bool Called { get; set; }

        public Task<List<string>> Validate(ICollection<X509Certificate2> certificates,
            ICollection<X509Certificate2>? rootCertificates, bool useSystemStore)
        {
            Called = true;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeErrorHandler : IErrorHandler
    {
        public List<Exception> Handled { get; } = new();
        public List<string?> Contexts { get; } = new();

        public Task HandleAsync(Exception exception, string? context = null)
        {
            Handled.Add(exception);
            Contexts.Add(context);
            return Task.CompletedTask;
        }
    }

    private readonly TrustStoreViewModel _vm;
    private readonly FakeService _service;
    private readonly FakeValidator _validator;
    private readonly FakeErrorHandler _errorHandler;

    public TrustStoreViewModelTests()
    {
        _service = new FakeService();
        _validator = new FakeValidator();
        _errorHandler = new FakeErrorHandler();
        _vm = new TrustStoreViewModel(_service, _validator, _errorHandler);
    }

    public void Dispose() => _vm.Dispose();

    [Fact]
    public async Task AddTrustedFiles_Success_AddsCertificates()
    {
        var cert = TestCertificates.SelfSigned();
        _service.Result = new DialogResult
        {
            Certificates = new List<X509Certificate2> { cert },
            Success = DialogResult.OperationResult.Success,
            Type = CertificateType.Pem
        };

        var path = WriteTempFile();
        await _vm.AddTrustedFilesAsync(new List<string> { path });

        Assert.Single(_vm.TrustedCertificates);
        Assert.Same(cert, _vm.TrustedCertificates[0]);
        Assert.Empty(_errorHandler.Handled);
        File.Delete(path);
    }

    [Fact]
    public async Task AddTrustedFiles_Failure_SurfacesThroughErrorHandler()
    {
        var boom = new InvalidOperationException("nope");
        _service.Result = DialogResult.CreateFail(boom);

        var path = WriteTempFile();
        await _vm.AddTrustedFilesAsync(new List<string> { path });

        Assert.Empty(_vm.TrustedCertificates);
        Assert.Single(_errorHandler.Handled);
        Assert.Same(boom, _errorHandler.Handled[0]);
        Assert.Equal("Adding root certificate", _errorHandler.Contexts[0]);
        File.Delete(path);
    }

    [Fact]
    public async Task AddTrustedFiles_Canceled_DoesNothing()
    {
        _service.Result = DialogResult.OperationCanceled();

        var path = WriteTempFile();
        await _vm.AddTrustedFilesAsync(new List<string> { path });

        Assert.Empty(_vm.TrustedCertificates);
        Assert.Empty(_errorHandler.Handled);
        File.Delete(path);
    }

    [Fact]
    public async Task RemoveFromTrustedStore_RemovesCertificate()
    {
        var cert = TestCertificates.SelfSigned();
        _vm.TrustedCertificates.Add(cert);

        await _vm.RemoveFromTrustedStore(cert);

        Assert.Empty(_vm.TrustedCertificates);
    }

    [Fact]
    public async Task ChainValidates_NoErrors_IsChainValidTrue()
    {
        _validator.Result = new List<string>();

        await SetChainAndAwaitValidity(new List<X509Certificate2> { TestCertificates.SelfSigned() });

        Assert.True(_validator.Called);
        Assert.True(_vm.IsChainValid);
        Assert.Empty(_vm.Errors);
    }

    [Fact]
    public async Task ChainFails_ErrorsListed_IsChainValidFalse()
    {
        _validator.Result = new List<string> { "Untrusted certificate" };

        await SetChainAndAwaitValidity(new List<X509Certificate2> { TestCertificates.SelfSigned() });

        Assert.False(_vm.IsChainValid);
        Assert.Contains("Untrusted certificate", _vm.Errors);
    }

    [Fact]
    public async Task EmptyChain_NoValidationCall()
    {
        await Task.Delay(100);
        Assert.False(_validator.Called);
        Assert.True(_vm.IsChainValid);
    }

    private async Task SetChainAndAwaitValidity(List<X509Certificate2> chain)
    {
        _vm.CertificateChain = new ObservableCollection<X509Certificate2>(chain);
        // UpdateValidity is fire-and-forget inside the VM; wait for the validator call.
        for (var i = 0; i < 100 && !_validator.Called; i++)
        {
            await Task.Delay(20);
        }

        // Give the continuation a beat to finish writing errors/validity.
        await Task.Delay(50);
    }

    private static string WriteTempFile()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"truststore_{Guid.NewGuid():N}.crt");
        System.IO.File.WriteAllBytes(path, TestCertificates.ToPem(TestCertificates.SelfSigned()));
        return path;
    }
}
