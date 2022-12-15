using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CertificateViewer.CertificateImporters;
using CertificateViewer.Dialogs;
using ReactiveUI;

namespace CertificateViewer;

public sealed class MainWindowVm : BaseViewModel, IDisposable
{
    private readonly CertificateManager certificateManager = new();
    private readonly ImportFromFileHelper importFromFileHelper = new();

    private CertificateVm? selectedItem;


    public MainWindowVm()
    {
        OpenCommand = ReactiveCommand.CreateFromTask(x => OpenFile(x));
        AddRootCommand = ReactiveCommand.CreateFromTask(AddRootCertificate);
        RemoveRootCommand = ReactiveCommand.Create(RemoveRootCertificate);
        OpenUrlCommand = ReactiveCommand.CreateFromTask(o => OpenUrl());

        ShowMessageBox = new Interaction<(string Title, string Message), MessageBox.MessageBoxResult>();
        ShowOpenFileDialog = new Interaction<string, string?>();
        ShowOpenUrlDialog = new Interaction<string, string?>();
    }
    public Interaction<string, string?> ShowOpenFileDialog { get; set; }
    public Interaction<string, string?> ShowOpenUrlDialog { get; set; }

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> AddRootCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveRootCommand { get; }

    public Interaction<(string Title, string Message), MessageBox.MessageBoxResult> ShowMessageBox { get; set; }
    public ObservableCollection<CertificateVm> Certificates => certificateManager.Certificates;

    public CertificateVm? SelectedRootCertificate { get; set; }

    public CertificateVm? SelectedItem
    {
        get => selectedItem;
        set => this.RaiseAndSetIfChanged(ref selectedItem, value);
    }


    public bool? IsValid => certificateManager.IsValid;

    public CertificateType CertificateType { get; set; }

    public ObservableCollection<CertificateVm> RootCertificates => certificateManager.RootCertificates;
    public ReactiveCommand<Unit, Unit> OpenUrlCommand { get; }

    public ObservableCollection<string> Errors => certificateManager.Errors;
    public bool UseSystemStore
    {
        get => certificateManager.UseSystemStore;
        set
        {
            certificateManager.UseSystemStore = value;
            this.RaisePropertyChanged(nameof(CertificateType));
            this.RaisePropertyChanged(nameof(IsValid));
            this.RaisePropertyChanged(nameof(Errors));
            this.RaisePropertyChanged(nameof(UseSystemStore));
        }
    }

    public void Dispose() => certificateManager.Dispose();

    private async Task OpenUrl()
    {
        try
        {
            var url = await ShowOpenUrlDialog.Handle(string.Empty);
            if (url == null)
            {
                return;
            }
            var result = await ImportFromUrlService.OpenFromUrlAsync(url);
            LoadCertificates(result);
        }
        catch (Exception e)
        {
            await ShowMessageBox.Handle(("Error opening URL", e.Message));
        }
    }

    private async Task OpenFile(object value)
    {
        try
        {
            var fileDialogResult = await ShowOpenFileDialog.Handle(String.Empty);
            if (fileDialogResult == null)
            {
                return;
            }
            var result = importFromFileHelper.LoadCertificate(fileDialogResult);
            LoadCertificates(result);
        }
        catch (Exception e)
        {
            await ShowMessageBox.Handle(("Error opening URL", e.Message));
        }
    }

    private void LoadCertificates(OperationResult operationResult)
    {
        if (operationResult.Success)
        {
            certificateManager.LoadCertificates(operationResult.Certificates!.ToList());
            CertificateType = operationResult.Type;
            this.RaisePropertyChanged(nameof(CertificateType));
            this.RaisePropertyChanged(nameof(IsValid));
            this.RaisePropertyChanged(nameof(Errors));
        }
        else
        {
            throw operationResult.Error!;
        }
    }

    private async Task AddRootCertificate()
    {
        try
        {
            var fileDialogResult = await ShowOpenFileDialog.Handle(String.Empty);
            if (fileDialogResult == null)
            {
                return;
            }
            var result = importFromFileHelper.LoadCertificate(fileDialogResult);
            if (result.Success)
            {
                var vm = result.Certificates!.Select(x => new CertificateVm(x)).Single();
                certificateManager.RootCertificates.Add(vm);
                this.RaisePropertyChanged(nameof(RootCertificates));
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }
        catch (Exception e)
        {
            await ShowMessageBox.Handle(("Error loading file", e.Message));
        }
    }

    private void RemoveRootCertificate()
    {
        if (SelectedRootCertificate is null)
        {
            return;
        }
        RootCertificates.Remove(SelectedRootCertificate);
        this.RaisePropertyChanged(nameof(RootCertificates));
        this.RaisePropertyChanged(nameof(IsValid));
    }
}
