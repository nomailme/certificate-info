using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CertificateViewer.CertificateImporters;
using CertificateViewer.Dialogs;
using Microsoft.Win32;
using MugenMvvmToolkit;
using MugenMvvmToolkit.Models;

namespace CertificateViewer;

public sealed class MainWindowVm2 : INotifyPropertyChanged, IDisposable
{
    private readonly ImportFromFileHelper importFromFileHelper = new();
    private readonly CertificateManager certificateManager = new();

    private CertificateVm? selectedItem;


    public MainWindowVm2()
    {
        OpenCommand = new RelayCommand(OpenFile);
        AddRootCommand = new RelayCommand(AddRootCertificate);
        RemoveRootCommand = new RelayCommand(RemoveRootCertificate);
        OpenUrlCommand = new RelayCommand( async o => await OpenUrl());
    }

    public ICommand OpenCommand { get; }
    public ICommand AddRootCommand { get; }
    public ICommand RemoveRootCommand { get; }
    public ObservableCollection<CertificateVm> Certificates => certificateManager.Certificates;

    public CertificateVm? SelectedRootCertificate { get; set; }

    public CertificateVm? SelectedItem
    {
        get => selectedItem;
        set => SetField(ref selectedItem, value);
    }


    public bool? IsValid => certificateManager.IsValid;

    public CertificateType CertificateType { get; set; }

    public ObservableCollection<CertificateVm> RootCertificates => certificateManager.RootCertificates;
    public ICommand OpenUrlCommand { get; }

    public ObservableCollection<string> Errors => certificateManager.Errors;
    public bool UseSystemStore
    {
        get => certificateManager.UseSystemStore;
        set
        {
            certificateManager.UseSystemStore = value;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(Errors));
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task OpenUrl()
    {
        try
        {
            var openUrlWindow = new OpenUrlWindow { DataContext = new Uri("https://google.com") };
            if (openUrlWindow.ShowDialog() != true)
            {
                return;
            }
            var result = await ImportFromUrlService.OpenFromUrlAsync(openUrlWindow.Url!);
            LoadCertificates(result);
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
            var result = importFromFileHelper.LoadCertificate(fileDialog.FileName);
            LoadCertificates(result);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error loading file");
        }
    }

    private void LoadCertificates(OperationResult operationResult)
    {
        if (operationResult.Success)
        {
            certificateManager.LoadCertificates(operationResult.Certificates!.ToList());
            CertificateType = operationResult.Type;
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(CertificateType));
            OnPropertyChanged(nameof(Errors));
        }
        else
        {
            throw operationResult.Error!;
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
            var result = importFromFileHelper.LoadCertificate(fileDialog.FileName);
            if (result.Success)
            {
                var vm = result.Certificates!.Select(x=>new CertificateVm(x)).Single();
                certificateManager.RootCertificates.Add(vm);
                OnPropertyChanged(nameof(RootCertificates));
                OnPropertyChanged(nameof(IsValid));
            }
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
        RootCertificates.Remove(SelectedRootCertificate);
        OnPropertyChanged(nameof(RootCertificates));
        OnPropertyChanged(nameof(IsValid));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        OnPropertyChanged(propertyName);
    }
    public void Dispose() => certificateManager.Dispose();
}
