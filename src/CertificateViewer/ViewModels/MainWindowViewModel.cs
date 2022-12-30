using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using ReactiveUI;

namespace CertificateViewer.ViewModels;

public sealed class MainWindowViewModel : BaseViewModel
{
    private readonly CertificateManager _certificateManager = new();
    private readonly OpenCertificateService _openCertificateService = new();

    private X509Certificate2? selectedItem;
    private X509Certificate2? selectedRootCertificate;


    public MainWindowViewModel()
    {
        var systemStoreDisabled = this.WhenAnyValue(vm => vm.UseSystemStore).Select(x => x == false);
        var rootCertificateSelected = this.WhenAnyValue(x => x.SelectedRootCertificate).Select(x => x is not null);
        var canRemoveRootCertificate = rootCertificateSelected.CombineLatest(systemStoreDisabled, (first, second) => first && second);
        var canAddRootCertificate = systemStoreDisabled;

        OpenCommand = ReactiveCommand.CreateFromTask(_ => OpenFile());
        AddRootCommand = ReactiveCommand.CreateFromTask(AddRootCertificate, canAddRootCertificate);
        RemoveRootCommand = ReactiveCommand.Create<X509Certificate2>(RemoveRootCertificate, canRemoveRootCertificate);
        OpenUrlCommand = ReactiveCommand.CreateFromTask(_ => OpenUrl());

        ShowMessageBox = new Interaction<(string Title, string Message), MessageBox.MessageBoxResult>();
        ShowOpenFileDialog = new Interaction<string, string?>();
        ShowOpenUrlDialog = new Interaction<string, string?>();
    }

    public Interaction<string, string?> ShowOpenFileDialog { get; }

    public Interaction<string, string?> ShowOpenUrlDialog { get; }

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> AddRootCommand { get; }
    public ReactiveCommand<X509Certificate2, Unit> RemoveRootCommand { get; }

    public Interaction<(string Title, string Message), MessageBox.MessageBoxResult> ShowMessageBox { get; set; }
    public ObservableCollection<X509Certificate2> Certificates => _certificateManager.Certificates;

    public X509Certificate2? SelectedRootCertificate
    {
        get => selectedRootCertificate;
        set => this.RaiseAndSetIfChanged(ref selectedRootCertificate, value);
    }

    public X509Certificate2? SelectedItem
    {
        get => selectedItem;
        set => this.RaiseAndSetIfChanged(ref selectedItem, value);
    }


    public bool? IsValid => _certificateManager.IsValid;

    public CertificateType CertificateType { get; set; }

    public ObservableCollection<X509Certificate2> RootCertificates => _certificateManager.RootCertificates;
    public ReactiveCommand<Unit, Unit> OpenUrlCommand { get; }

    public ObservableCollection<string> Errors => _certificateManager.Errors;
    public bool UseSystemStore
    {
        get => _certificateManager.UseSystemStore;
        set
        {
            _certificateManager.UseSystemStore = value;
            this.RaisePropertyChanged(nameof(CertificateType));
            this.RaisePropertyChanged(nameof(IsValid));
            this.RaisePropertyChanged(nameof(Errors));
            this.RaisePropertyChanged();
        }
    }

    private async Task OpenUrl()
    {
        var remoteServerCertificateImporter = new RemoteServerImporter();
        try
        {
            var url = await ShowOpenUrlDialog.Handle(string.Empty);
            if (url == null)
            {
                return;
            }
            var result = await remoteServerCertificateImporter.ImportAsync(url);
            LoadCertificates(result.ToDialogResult(CertificateType.Web));
        }
        catch (Exception e)
        {
            await ShowMessageBox.Handle(("Error opening URL", e.Message));
        }
    }

    private async Task OpenFile()
    {
        var fileDialogResult = await ShowOpenFileDialog.Handle(string.Empty);
        if (fileDialogResult is null)
        {
            return;
        }
        var result = await _openCertificateService.OpenFile(fileDialogResult);
        LoadCertificates(result);
    }

    private void LoadCertificates(DialogResult dialogResult)
    {
        switch (dialogResult.Success)
        {
            case DialogResult.OperationResult.Success:
                _certificateManager.LoadCertificates(dialogResult.Certificates!.ToList());
                CertificateType = dialogResult.Type;
                this.RaisePropertyChanged(nameof(CertificateType));
                this.RaisePropertyChanged(nameof(IsValid));
                this.RaisePropertyChanged(nameof(Errors));
                return;
            case DialogResult.OperationResult.Canceled:
                return;
            default:
                ShowMessageBox.Handle(("Unable to open file", dialogResult.Error!.Message));
                break;
        }
    }

    private async Task AddRootCertificate()
    {
        try
        {
            var fileDialogResult = await ShowOpenFileDialog.Handle(string.Empty);
            if (fileDialogResult is null)
            {
                return;
            }
            var result = await _openCertificateService.OpenFile(fileDialogResult);
            if (result.Success == DialogResult.OperationResult.Success)
            {
                var vm = result.Certificates!.Single();
                _certificateManager.RootCertificates.Add(vm);
                this.RaisePropertyChanged(nameof(RootCertificates));
                this.RaisePropertyChanged(nameof(IsValid));
                this.RaisePropertyChanged(nameof(Errors));
            }
        }
        catch (Exception e)
        {
            await ShowMessageBox.Handle(("Error loading file", e.Message));
        }
    }

    private void RemoveRootCertificate(X509Certificate2 certificate)
    {
        RootCertificates.Remove(certificate!);
        this.RaisePropertyChanged(nameof(RootCertificates));
        this.RaisePropertyChanged(nameof(IsValid));
        this.RaisePropertyChanged(nameof(SelectedRootCertificate));
    }
}
