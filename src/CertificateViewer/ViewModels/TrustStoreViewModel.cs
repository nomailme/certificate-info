using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Logic;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace CertificateViewer.ViewModels;

/// <summary>
///     View model for the trust-store panel: owns the trusted-certificate collection, the
///     validity state and the add/remove commands. The panel itself is a dumb view bound to
///     this model; its <see cref="Avalonia.Controls.UserControl.DataContext" /> is set by the host.
/// </summary>
public partial class TrustStoreViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private readonly OpenCertificateService _openCertificateService;
    private readonly ICertificateChainValidator _chainValidator;
    private readonly IErrorHandler _errorHandler;
    private readonly CompositeDisposable _disposables = new();
    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private ObservableCollection<X509Certificate2> _trustedCertificates = new();
    private ObservableCollection<string> _errors = new();
    private bool _isChainValid = true;
    private bool _useSystemStore = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TrustStoreViewModel" /> class.
    /// </summary>
    /// <param name="openCertificateService">Service used to load certificates from files.</param>
    /// <param name="chainValidator">Validator used to build and validate the chain against a trust store.</param>
    /// <param name="errorHandler">Handler used to surface file-loading errors to the user.</param>
    public TrustStoreViewModel(
        OpenCertificateService openCertificateService,
        ICertificateChainValidator chainValidator,
        IErrorHandler errorHandler)
    {
        Activator = new ViewModelActivator();
        _openCertificateService = openCertificateService;
        _chainValidator = chainValidator;
        _errorHandler = errorHandler;

        // The panel used to subscribe with an undisposed async-void subscription; the VM owns
        // the subscription and disposes it on deactivation.
        var validityChanges = this.WhenAnyValue(
            x => x.TrustedCertificates,
            x => x.CertificateChain,
            x => x.UseSystemStore,
            (_, _, _) => true);
        validityChanges.Subscribe(u => { _ = UpdateValidity(); }).DisposeWith(_disposables);

        Activator.Deactivated
            .Subscribe(_ => _disposables.Dispose())
            .DisposeWith(_disposables);
    }

    /// <summary>Gets the view-model activator.</summary>
    public ViewModelActivator Activator { get; }

    /// <summary>Gets or sets the certificate chain whose validity is displayed.</summary>
    public ObservableCollection<X509Certificate2> CertificateChain
    {
        get => _certificateChain;
        set => this.RaiseAndSetIfChanged(ref _certificateChain, value);
    }

    /// <summary>Gets or sets the certificates trusted by the custom store.</summary>
    public ObservableCollection<X509Certificate2> TrustedCertificates
    {
        get => _trustedCertificates;
        set => this.RaiseAndSetIfChanged(ref _trustedCertificates, value);
    }

    /// <summary>Gets or sets the validation errors for the current chain.</summary>
    public ObservableCollection<string> Errors
    {
        get => _errors;
        set => this.RaiseAndSetIfChanged(ref _errors, value);
    }

    /// <summary>Gets or sets whether the current chain validates.</summary>
    public bool IsChainValid
    {
        get => _isChainValid;
        set => this.RaiseAndSetIfChanged(ref _isChainValid, value);
    }

    /// <summary>Gets or sets whether the system trust store is used for validation.</summary>
    public bool UseSystemStore
    {
        get => _useSystemStore;
        set => this.RaiseAndSetIfChanged(ref _useSystemStore, value);
    }

    /// <summary>
    ///     Adds one or more certificates from files to the custom trusted store.
    ///     File-loading failures are surfaced through the injected error handler instead of
    ///     being silently dropped.
    /// </summary>
    [RelayCommand]
    public async Task AddRootCertificate()
    {
        var files = await this.OpenFileDialogAsync("Root certificates");

        foreach (var file in files)
        {
            var result = await _openCertificateService.OpenFile(file);
            if (result.Success == DialogResult.OperationResult.Success)
            {
                foreach (var certificate in result.Certificates)
                {
                    TrustedCertificates.Add(certificate);
                }
            }
            else
            {
                _ = _errorHandler.HandleAsync(
                    result.Error ?? new InvalidOperationException("Unable to load certificate"),
                    context: "Adding root certificate");
            }
        }
    }

    /// <summary>Removes a certificate from the custom trusted store.</summary>
    /// <param name="certificate">The certificate to remove.</param>
    [RelayCommand]
    public Task RemoveFromTrustedStore(X509Certificate2 certificate)
    {
        TrustedCertificates.Remove(certificate);
        return Task.CompletedTask;
    }

    /// <summary>Disposes the validity subscription.</summary>
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task UpdateValidity()
    {
        if (CertificateChain.Count == 0)
        {
            return;
        }

        var result = await _chainValidator.Validate(CertificateChain, TrustedCertificates, UseSystemStore);
        Errors.Clear();
        foreach (var error in result)
        {
            Errors.Add(error);
        }

        IsChainValid = result.Count == 0;
    }
}
