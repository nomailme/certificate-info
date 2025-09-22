using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using CertificateViewer.ViewModels;
using ReactiveUI;
using ShadUI;
using OpenUrlViewModel2 = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlViewModel2;

namespace CertificateViewer.Components.MainWindow2;

public class MainWindow2ViewModel : BaseViewModel
{
    private readonly ThemeWatcher _themeWatcher;
    private readonly ViewModelFactory _viewModelFactory;
    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private string _certificateSource = string.Empty;
    private ThemeMode _currentTheme;
    private ShadUI.DialogManager _dialogManager;
    private bool _isBusy;
    private OpenCertificateService _openCertificateService;
    private X509Certificate2? _selectedCertificate;
    private string _title = string.Empty;

    public MainWindow2ViewModel(
        ShadUI.DialogManager dialogManager,
        PasswordDialogViewModel passwordViewModel,
        ViewModelFactory viewModelFactory,
        ThemeWatcher themeWatcher)
    {
        ShowOpenFileDialog = new Interaction<string, string?>();
        _openCertificateService = new OpenCertificateService(dialogManager, passwordViewModel);
        _dialogManager = dialogManager;
        _viewModelFactory = viewModelFactory;
        OpenCertificateFileCommand = ReactiveCommand.CreateFromTask(_ => OpenFile());
        GetCertificateFromUrlCommand = ReactiveCommand.Create(OpenUrl);
        SwitchThemeCommand = ReactiveCommand.CreateFromTask(_ => SwitchTheme());
        _themeWatcher = themeWatcher;
    }
    public Interaction<string, string?> ShowOpenFileDialog { get; set; }

    public ReactiveCommand<Unit, Unit> SwitchThemeCommand { get; set; }

    public ReactiveCommand<Unit, Unit> GetCertificateFromUrlCommand { get; set; }

    public ReactiveCommand<Unit, Unit> OpenCertificateFileCommand { get; set; }

    public string Title { get => _title; set => this.RaiseAndSetIfChanged(ref _title, value); }

    public ObservableCollection<X509Certificate2> CertificateChain
    {
        get => _certificateChain;
        set => this.RaiseAndSetIfChanged(ref _certificateChain, value);
    }

    public ShadUI.DialogManager DialogManager
    {
        get => _dialogManager;
        set => this.RaiseAndSetIfChanged(ref _dialogManager, value);
    }

    public OpenCertificateService OpenCertificateService
    {
        get => _openCertificateService;
        set => this.RaiseAndSetIfChanged(ref _openCertificateService, value);
    }

    public X509Certificate2? SelectedCertificate
    {
        get => _selectedCertificate;
        set => this.RaiseAndSetIfChanged(ref _selectedCertificate, value);
    }
    public string CertificateSource { get => _certificateSource; set => this.RaiseAndSetIfChanged(ref _certificateSource, value); }

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

    private void OpenUrl()
    {
        var openUrlViewModel = _viewModelFactory.Build<OpenUrlViewModel2>();
        DialogManager.CreateDialog(openUrlViewModel)
            .WithSuccessCallback(vm => Load(vm.Url))
            .WithCancelCallback(() => { })
            .Show();

        string GetDomain(string uri)
        {
            var url = new Uri(uri);
            return $"url:{url.Host}";
        }

        async Task Load(string urlInput)
        {
            using (BusyObject.Create(() => IsBusy = true, () => IsBusy = false))
            {
                var remoteServerCertificateImporter = new RemoteServerImporter();
                try
                {

                    if (string.IsNullOrWhiteSpace(urlInput))
                    {
                        return;
                    }

                    var result = await remoteServerCertificateImporter.ImportAsync(urlInput);
                    if (!result.Success)
                    {
                        throw result.Error ?? new InvalidOperationException("Error loading certificate");
                    }

                    CertificateSource = GetDomain(urlInput);
                    LoadCertificates(result.ToDialogResult(CertificateType.Web));
                    SetTitle(urlInput);
                }
                catch (Exception e)
                {
                    _dialogManager.CreateDialog("Error loading file", e.Message)
                        .Dismissible()
                        .Show();
                }
            }
        }

    }

    private void SetTitle(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Title = "Certificate Viewer";
        }

        Title = $"Certificate Viewer: {input}";
    }

    private async Task OpenFile()
    {
        var fileDialogResult = await this.OpenFileDialogAsync("Open file", false);
        var result = await _openCertificateService.OpenFile(fileDialogResult.Single());
        CertificateSource = GetFileName(fileDialogResult.Single());
        LoadCertificates(result);
        SetTitle(fileDialogResult.Single());

        string GetFileName(string input)
        {
            return $"file:{Path.GetFileName(input)}";
        }
    }
    private void LoadCertificates(DialogResult dialogResult)
    {
        switch (dialogResult.Success)
        {
            case DialogResult.OperationResult.Success:

                CertificateChain = new ObservableCollection<X509Certificate2>(dialogResult.Certificates.ToList());
                SelectedCertificate = dialogResult.Certificates.First();
                break;
            case DialogResult.OperationResult.Canceled:
                break;
            default:
                _dialogManager.CreateDialog("Unable to open file", dialogResult.Error!.Message)
                    .Dismissible()
                    .Show();
                break;
        }
    }

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
