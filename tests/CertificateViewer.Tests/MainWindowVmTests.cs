using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia;
using CertificateViewer.Components.MainWindow;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using CertificateViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Headless unit tests for the main-window view model (Phase 3: VM-level test strategy).
///     The file service and error handler are fakes, so no UI and no network are involved; the
///     load paths are exercised directly through the public async API.
/// </summary>
public class MainWindowVmTests : IDisposable
{
    private sealed class FakeService : IOpenCertificateService
    {
        public DialogResult Result { get; set; } = DialogResult.OperationCanceled();

        public Task<DialogResult> OpenFile(string path) => Task.FromResult(Result);
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

    private readonly MainWindowVm _vm;
    private readonly FakeService _service;
    private readonly FakeErrorHandler _errorHandler;

    public MainWindowVmTests()
    {
        TestPlatform.EnsureInitialized();
        _service = new FakeService();
        _errorHandler = new FakeErrorHandler();
        var provider = new ServiceCollection().BuildServiceProvider();
        _vm = new MainWindowVm(
            new DialogManager(),
            new ViewModelFactory(provider),
            new ThemeWatcher(Application.Current!),
            _errorHandler,
            _service,
            new CertificateSourceLoader(new Logic.ImportServices.Implementation.RemoteServerImporter(), _errorHandler),
            new TrustStoreViewModel(_service, new ChainValidator(), _errorHandler),
            new CertificateChainViewModel(),
            new CertificateDetailsViewModel());
    }

    public void Dispose() => _vm.Dispose();

    [Fact]
    public async Task LoadFileAsync_Success_LoadsChainAndSource()
    {
        var cert = TestCertificates.SelfSigned();
        _service.Result = new DialogResult
        {
            Certificates = new List<X509Certificate2> { cert },
            Success = DialogResult.OperationResult.Success,
            Type = CertificateType.Pem
        };

        await _vm.LoadFileAsync("/tmp/certtest/somefile.crt");

        Assert.Single(_vm.CertificateChain);
        Assert.Same(cert, _vm.SelectedCertificate);
        Assert.Equal("file:somefile.crt", _vm.CertificateSource);
        Assert.Contains("somefile.crt", _vm.Title);
        Assert.Empty(_errorHandler.Handled);
    }

    [Fact]
    public async Task LoadFileAsync_EmptySuccess_KeepsSelectionEmpty()
    {
        _service.Result = new DialogResult
        {
            Certificates = new List<X509Certificate2>(),
            Success = DialogResult.OperationResult.Success,
            Type = CertificateType.Pem
        };

        await _vm.LoadFileAsync("/tmp/empty.pem");

        Assert.Empty(_vm.CertificateChain);
        Assert.Null(_vm.SelectedCertificate);
        Assert.Empty(_errorHandler.Handled);
    }

    [Fact]
    public async Task LoadFileAsync_Failure_SurfacesThroughErrorHandler()
    {
        var boom = new InvalidOperationException("bad file");
        _service.Result = DialogResult.CreateFail(boom);

        await _vm.LoadFileAsync("/tmp/bad.bin");

        Assert.Single(_errorHandler.Handled);
        Assert.Same(boom, _errorHandler.Handled[0]);
    }

    [Fact]
    public async Task LoadFileAsync_Canceled_DoesNothing()
    {
        _service.Result = DialogResult.OperationCanceled();

        await _vm.LoadFileAsync("/tmp/x.pem");

        Assert.Empty(_vm.CertificateChain);
        Assert.Empty(_errorHandler.Handled);
    }

    [Fact]
    public async Task HandleFileOpenError_SurfacesThroughErrorHandler()
    {
        var boom = new UnauthorizedAccessException("denied");

        await _vm.HandleFileOpenError(boom);

        Assert.Single(_errorHandler.Handled);
        Assert.Same(boom, _errorHandler.Handled[0]);
    }

    [Fact]
    public void SelectedCertificate_SyncsToDetailsViewModel()
    {
        var cert = TestCertificates.SelfSigned();

        _vm.SelectedCertificate = cert;

        Assert.Same(cert, _vm.CertificateDetailsViewModel.Certificate);
    }

    [Fact]
    public void SelectedCertificate_EqualValue_DoesNotNotify()
    {
        var cert = TestCertificates.SelfSigned();
        _vm.SelectedCertificate = cert;

        var notifications = 0;
        _vm.PropertyChanged += (sender, args) =>
            notifications += (args.PropertyName == nameof(MainWindowVm.SelectedCertificate)) ? 1 : 0;

        _vm.SelectedCertificate = cert;

        Assert.Equal(0, notifications);
    }

    [Fact]
    public void CertificateSource_UpdatesChainViewModelSource()
    {
        _vm.CertificateSource = "url:example.com";

        Assert.Equal("url:example.com", _vm.CertificateChainViewModel.CertificateSource);
    }

    [Fact]
    public void IsBusy_RaisesPropertyChanged()
    {
        var raised = false;
        _vm.PropertyChanged += (sender, args) =>
            raised = args.PropertyName == nameof(MainWindowVm.IsBusy);

        _vm.IsBusy = true;

        Assert.True(raised);
    }

    [Fact]
    public void Title_InitialState_UsesDefaultVersionTitle()
    {
        // In the initial state (empty source) the title is the plain "Certificate Viewer" title
        // with no source suffix.
        Assert.StartsWith("Certificate Viewer", _vm.Title);
        Assert.DoesNotContain("somefile", _vm.Title);
    }
}
