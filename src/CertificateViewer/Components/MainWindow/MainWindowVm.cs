using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using CertificateViewer.ViewModels;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.MainWindow;

/// <summary>
///     Orchestrator view model for the main window: holds the chain/selection/title/busy/theme
///     state and thin commands. Child-panel view models are constructed in the composition root
///     (App) and passed in; services are used internally and are not exposed as bindable
///     properties.
/// </summary>
public partial class MainWindowVm : ReactiveObject, IActivatableViewModel, IDisposable
{
    private readonly IErrorHandler _errorHandler;
    private readonly CompositeDisposable _disposables = new();
    private readonly ThemeWatcher _themeWatcher;
    private readonly ViewModelFactory _viewModelFactory;
    private readonly ShadUI.DialogManager _dialogManager;
    private readonly OpenCertificateService _openCertificateService;
    private readonly CertificateSourceLoader _certificateSourceLoader;
    private readonly TrustStoreViewModel _trustStoreViewModel;
    private readonly CertificateChainViewModel _certificateChainViewModel;
    private readonly CertificateDetailsViewModel _certificateDetailsViewModel;
    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private string _certificateSource = string.Empty;
    private ThemeMode _currentTheme;
    private bool _isBusy;
    private X509Certificate2? _selectedCertificate;
    private string _title = string.Empty;

    public MainWindowVm(
        ShadUI.DialogManager dialogManager,
        ViewModelFactory viewModelFactory,
        ThemeWatcher themeWatcher,
        IErrorHandler errorHandler,
        OpenCertificateService openCertificateService,
        CertificateSourceLoader certificateSourceLoader,
        TrustStoreViewModel trustStoreViewModel,
        CertificateChainViewModel certificateChainViewModel,
        CertificateDetailsViewModel certificateDetailsViewModel)
    {
        Activator = new ViewModelActivator();
        ShowOpenFileDialog = new Interaction<string, string?>();
        OpenFile = new Interaction<string, Unit>();
        _dialogManager = dialogManager;
        _viewModelFactory = viewModelFactory;
        _themeWatcher = themeWatcher;
        _errorHandler = errorHandler;
        _openCertificateService = openCertificateService;
        _certificateSourceLoader = certificateSourceLoader;
        _trustStoreViewModel = trustStoreViewModel;
        _certificateChainViewModel = certificateChainViewModel;
        _certificateDetailsViewModel = certificateDetailsViewModel;

        // The panel's ListBox selection flows back into SelectedCertificate (consumed by the
        // details panel); assigning an equal value is a no-op, so this cannot loop.
        _certificateChainViewModel
            .WhenAnyValue(x => x.SelectedCertificate)
            .Subscribe(u => SelectedCertificate = u)
            .DisposeWith(_disposables);

        SetTitle(string.Empty);
    }

    /// <summary>Gets the view-model activator.</summary>
    public ViewModelActivator Activator { get; }

    /// <summary>Gets the view model of the trust-store panel; the panel's DataContext.</summary>
    public TrustStoreViewModel TrustStoreViewModel => _trustStoreViewModel;

    /// <summary>Gets the view model of the certificate-chain panel; the panel's DataContext.</summary>
    public CertificateChainViewModel CertificateChainViewModel => _certificateChainViewModel;

    /// <summary>Gets the view model of the certificate-details panel; the panel's DataContext.</summary>
    public CertificateDetailsViewModel CertificateDetailsViewModel => _certificateDetailsViewModel;

    public Interaction<string, string?> ShowOpenFileDialog { get; set; }

    public Interaction<string, Unit> OpenFile { get; set; }

    public string Title { get => _title; set => this.RaiseAndSetIfChanged(ref _title, value); }

    public ObservableCollection<X509Certificate2> CertificateChain
    {
        get => _certificateChain;
        set => this.RaiseAndSetIfChanged(ref _certificateChain, value);
    }

    public X509Certificate2? SelectedCertificate
    {
        get => _selectedCertificate;
        set
        {
            if (_selectedCertificate != value)
            {
                this.RaiseAndSetIfChanged(ref _selectedCertificate, value);
                // Keep the chain panel's ListBox selection in sync; assigning an equal value
                // is a no-op on the chain VM, so the two-way sync cannot loop.
                _certificateChainViewModel.SelectedCertificate = value;
                // The details panel shows the same certificate (its VM is the panel's DataContext).
                _certificateDetailsViewModel.Certificate = value;
            }
        }
    }

    public string CertificateSource
    {
        get => _certificateSource;
        set
        {
            if (_certificateSource != value)
            {
                this.RaiseAndSetIfChanged(ref _certificateSource, value);
                _certificateChainViewModel.CertificateSource = value;
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        private set => this.RaiseAndSetIfChanged(ref _currentTheme, value);
    }

    [RelayCommand]
    private async Task OpenUrl()
    {
        var openUrlViewModel = _viewModelFactory.Build<OpenUrlVm>();
        _dialogManager.CreateDialog(openUrlViewModel)
            .WithSuccessCallback(vm => Load(vm.Url))
            .WithCancelCallback(() => { })
            .Show();

        // The URL-load logic (import, error mapping, source/title) lives in the
        // CertificateSourceLoader service; here we only apply the result to the UI state.
        async Task Load(string uri)
        {
            using (BusyObject.Create(() => IsBusy = true, () => IsBusy = false))
            {
                var loadResult = await _certificateSourceLoader.LoadAsync(uri);
                if (!loadResult.Success || loadResult.Certificates is null)
                {
                    return;
                }

                CertificateSource = loadResult.Source;
                LoadCertificates(new DialogResult
                {
                    Certificates = new List<X509Certificate2>(loadResult.Certificates),
                    Success = DialogResult.OperationResult.Success
                });
                SetTitle(loadResult.Title);
            }
        }
    }

    private void SetTitle(string certficateSource)
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(certficateSource))
        {
            Title = $"Certificate Viewer {version}";
        }

        Title = $"Certificate Viewer {version}: {certficateSource}";
    }

    [RelayCommand]
    private async Task OpenCertificateFile()
    {
        var fileDialogResult = await this.OpenFileDialogAsync("Open file", false);
        if (fileDialogResult.Count == 0)
        {
            return;
        }
        await LoadFileAsync(fileDialogResult.Single());
    }

    private static string GetFileSource(string input) => $"file:{Path.GetFileName(input)}";

    public async Task LoadFileAsync(string filepath)
    {
        CertificateSource = GetFileSource(filepath);
        var result = await _openCertificateService.OpenFile(filepath);
        LoadCertificates(result);
        SetTitle(filepath);
    }

    /// <summary>
    ///     Surfaces a file-opening failure (e.g. from the window's drag-drop path) through the
    ///     single error path.
    /// </summary>
    /// <param name="error">The exception that occurred while opening the file.</param>
    public Task HandleFileOpenError(Exception error) =>
        _errorHandler.HandleAsync(error, context: "Opening file");

    private void LoadCertificates(DialogResult dialogResult)
    {
        switch (dialogResult.Success)
        {
            case DialogResult.OperationResult.Success:

                CertificateChain = new ObservableCollection<X509Certificate2>(dialogResult.Certificates.ToList());
                // Forward the chain to the trust-store view model so its validity subscription fires.
                _trustStoreViewModel.CertificateChain = CertificateChain;
                // Keep the chain panel's items in sync (same instance, so no notification needed).
                _certificateChainViewModel.Items = CertificateChain;
                // No-certificate case: do not throw from First(); leave selection empty.
                SelectedCertificate = dialogResult.Certificates.Count > 0 ? dialogResult.Certificates[0] : null;
                break;
            case DialogResult.OperationResult.Canceled:
                break;
            default:
                _ = _errorHandler.HandleAsync(
                    dialogResult.Error ?? new InvalidOperationException("Unknown error"),
                    context: "Unable to open file");
                break;
        }
    }

    /// <summary>Disposes the owned view models and subscriptions.</summary>
    public void Dispose()
    {
        _disposables.Dispose();
        _trustStoreViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private Task SwitchTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System
        };

        _themeWatcher.SwitchTheme(CurrentTheme);
        return Task.CompletedTask;
    }
}

public class BusyObject : IDisposable
{
    public required Action OnStart { get; set; }
    public required Action OnStop { get; set; }

    public void Dispose()
    {
        OnStop();
        GC.SuppressFinalize(this);
    }

    public static BusyObject Create(Action onStart, Action onStop)
    {
        var busyObject = new BusyObject
        {
            OnStart = onStart, OnStop = onStop
        };
        busyObject.OnStart();
        return busyObject;
    }
}
