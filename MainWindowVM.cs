using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MugenMvvmToolkit;
using MugenMvvmToolkit.Models;

namespace CertificateViewerPlayground;

public class MainWindowVm : INotifyPropertyChanged
{
    private readonly CertificateHelper certificateHelper = new();
    private readonly RootCertificateStore rootCertificateStore = new();

    private ObservableCollection<CertificateVm> certificateVms = new();
    private CertificateVm? selectedItem;


    public MainWindowVm()
    {
        OpenCommand = new RelayCommand(OpenFile);
        AddRootCommand = new RelayCommand(AddRootCertificate);
        RemoveRootCommand = new RelayCommand(RemoveRootCertificate);
        OpenUrlCommand = new RelayCommand(async x => await OpenUrl(x));
    }

    public ICommand OpenCommand { get; }
    public ICommand AddRootCommand { get; }
    public ICommand RemoveRootCommand { get; }
    public ObservableCollection<CertificateVm> Certificates
    {
        get => certificateVms;
        private set => SetField(ref certificateVms, value);
    }

    public X509Certificate2? SelectedRootCertificate { get; set; }

    public CertificateVm? SelectedItem
    {
        get => selectedItem;
        set => SetField(ref selectedItem, value);
    }

    public bool UseSystemStore
    {
        get => rootCertificateStore.UseSystemRootStore;
        set
        {
            rootCertificateStore.UseSystemRootStore = value;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged();
        }
    }

    public bool? IsValid
    {
        get
        {
            if (certificateVms.IsNullOrEmpty())
            {
                return null;
            }
            X509Certificate2[] certificates = certificateVms.Select(x => x.Certificate).Where(x => x != null).ToArray()!;
            var info = new CertificateFileInfo(new X509Certificate2Collection(certificates), rootCertificateStore);
            return info.Validate().IsEmpty();
        }
    }

    public CertificateType CertificateType { get; set; }

    public ObservableCollection<X509Certificate2> RootCertificates => new(rootCertificateStore.RootCertificates.ToList());
    public ICommand OpenUrlCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task OpenUrl(object obj)
    {
        try
        {
            var openUrlWindow = new OpenUrlWindow();
            openUrlWindow.ShowDialog();
            if (openUrlWindow.DialogResult != true)
            {
                return;
            }
            var retriever = new ServerCertificateRetriever();
            var certificates = await retriever.GetAsync(openUrlWindow.Text);
            var certificatesVm = certificates.Select(x => new CertificateVm(x)).ToList();
            Certificates = new ObservableCollection<CertificateVm>(certificatesVm);
            OnPropertyChanged(nameof(IsValid));
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error opening URL");
        }
    }

    private void OpenFile(object value)
    {
        try
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() != true)
            {
                return;
            }
            var result = certificateHelper.LoadCertificate(fileDialog.FileName);
            var certificatesVm = result.Certificates.Select(x => new CertificateVm(x)).ToList();
            Certificates = new ObservableCollection<CertificateVm>(certificatesVm);
            CertificateType = result.Type;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(CertificateType));
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error loading file");
        }
    }

    private void AddRootCertificate(object value)
    {
        try
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() != true)
            {
                return;
            }
            var result = certificateHelper.Open(fileDialog.FileName);
            rootCertificateStore.Add(result);
            OnPropertyChanged(nameof(RootCertificates));
            OnPropertyChanged(nameof(IsValid));
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error loading file");
        }
    }

    private void RemoveRootCertificate(object value)
    {
        if (SelectedRootCertificate is null)
        {
            return;
        }
        rootCertificateStore.Remove(SelectedRootCertificate);
        OnPropertyChanged(nameof(RootCertificates));
        OnPropertyChanged(nameof(IsValid));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
