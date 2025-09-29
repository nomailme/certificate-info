using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using CertificateViewer.ViewModels;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.MainWindow;

public partial class MainWindowVm : BaseViewModel, IActivatableViewModel
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

    public MainWindowVm(
        ShadUI.DialogManager dialogManager,
        PasswordDialogViewModel passwordViewModel,
        ViewModelFactory viewModelFactory,
        ThemeWatcher themeWatcher)
    {
        Activator = new ViewModelActivator();
        ShowOpenFileDialog = new Interaction<string, string?>();
        OpenFile = new Interaction<string, Unit>();
        _openCertificateService = new OpenCertificateService(dialogManager, passwordViewModel);
        _dialogManager = dialogManager;
        _viewModelFactory = viewModelFactory;
        _themeWatcher = themeWatcher;

        SetTitle(string.Empty);
    }


    public Interaction<string, string?> ShowOpenFileDialog { get; set; }

    public Interaction<string, Unit> OpenFile { get; set; }

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

    [RelayCommand]
    private void OpenUrl()
    {
        var openUrlViewModel = _viewModelFactory.Build<OpenUrlVm>();
        DialogManager.CreateDialog(openUrlViewModel)
            .WithSuccessCallback(vm => Load(vm.Url))
            .WithCancelCallback(() => { })
            .Show();

        string GetDomain(string uri)
        {
            var url = new Uri(uri);
            return $"url:{url.Host}";
        }

        async Task Load(string uri)
        {
            using (BusyObject.Create(() => IsBusy = true, () => IsBusy = false))
            {
                var remoteServerCertificateImporter = new RemoteServerImporter();
                try
                {

                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        return;
                    }

                    var result = await remoteServerCertificateImporter.ImportAsync(uri);
                    if (!result.Success)
                    {
                        throw result.Error ?? new InvalidOperationException("Error loading certificate");
                    }

                    CertificateSource = GetDomain(uri);
                    LoadCertificates(result.ToDialogResult(CertificateType.Web));
                    SetTitle(uri);
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
    public ViewModelActivator Activator { get; }

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
