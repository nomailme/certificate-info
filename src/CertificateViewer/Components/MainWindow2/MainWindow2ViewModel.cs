using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Extensions;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using CertificateViewer.ViewModels;
using ReactiveUI;

namespace CertificateViewer.Components.MainWindow2;

public class MainWindow2ViewModel : BaseViewModel
{
    private readonly OpenCertificateService _openCertificateService = new();
    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private string _certificateSource = string.Empty;
    private X509Certificate2? selectedCertificate;

    public MainWindow2ViewModel()
    {
        ShowOpenFileDialog = new Interaction<string, string?>();
        ShowOpenUrlDialog = new Interaction<string, string?>();
        OpenCertificateFileCommand = ReactiveCommand.CreateFromTask(_ => OpenFile());
        GetCertificateFromUrlCommand = ReactiveCommand.CreateFromTask(_ => OpenUrl());
    }

    public ReactiveCommand<Unit, Unit> GetCertificateFromUrlCommand { get; set; }

    public ReactiveCommand<Unit, Unit> OpenCertificateFileCommand { get; set; }

    public ObservableCollection<X509Certificate2> CertificateChain
    {
        get => _certificateChain;
        set => this.RaiseAndSetIfChanged(ref _certificateChain, value);
    }

    public Interaction<string, string?> ShowOpenFileDialog { get; }

    public Interaction<string, string?> ShowOpenUrlDialog { get; }



    public X509Certificate2? SelectedCertificate
    {
        get => selectedCertificate;
        set => this.RaiseAndSetIfChanged(ref selectedCertificate, value);
    }
    public string CertificateSource { get => _certificateSource; set => this.RaiseAndSetIfChanged(ref _certificateSource, value); }

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
            if (!result.Success)
            {
                throw result.Error ?? new InvalidOperationException("Error loading certificate");
            }

            CertificateSource = GetDomain(url);
            await LoadCertificates(result.ToDialogResult(CertificateType.Web));
        }
        catch (Exception e)
        {
            await this.ShowErrorMessage("Error opening URL", e.Message);
        }

        string GetDomain(string uri)
        {
            var url = new Uri(uri);
            return $"url:{url.Host}";
        }

    }

    private async Task OpenFile()
    {
        var fileDialogResult = await this.OpenFileDialogAsync("Open file", false);
        if (fileDialogResult is null)
        {
            return;
        }

        var result = await _openCertificateService.OpenFile(fileDialogResult.Single());
        CertificateSource = GetFileName(fileDialogResult.Single());
        await LoadCertificates(result);

        string GetFileName(string input)
        {
            return $"file:{Path.GetFileName(input)}";
        }
    }
    private async Task LoadCertificates(DialogResult dialogResult)
    {
        switch (dialogResult.Success)
        {
            case DialogResult.OperationResult.Success:

                CertificateChain =
                    new ObservableCollection<X509Certificate2>(dialogResult.Certificates.ToList());
                SelectedCertificate = dialogResult.Certificates.First();
                return;
            case DialogResult.OperationResult.Canceled:
                return;
            default:
                await this.ShowErrorMessage("Unable to open file", dialogResult.Error!.Message);
                break;
        }
    }
}
