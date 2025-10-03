using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Logic;
using CertificateViewer.Services;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.TrustStore;

public partial class TrustStorePanel : UserControl
{

    public static readonly DirectProperty<TrustStorePanel, ObservableCollection<X509Certificate2>>
        CertificateChainProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, ObservableCollection<X509Certificate2>>(
                nameof(CertificateChain),
                o => o.CertificateChain,
                (o, v) => o.CertificateChain = v);

    public static readonly DirectProperty<TrustStorePanel, ObservableCollection<string>>
        ErrorsProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, ObservableCollection<string>>(
                nameof(Errors),
                o => o.Errors);

    public static readonly DirectProperty<TrustStorePanel, bool>
        UseSystemStoreProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, bool>(
                nameof(UseSystemStore),
                o => o.UseSystemStore,
                (o, v) => o.UseSystemStore = v);

    public static readonly DirectProperty<TrustStorePanel, bool>
        IsChainValidProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, bool>(
                nameof(IsChainValid),
                o => o.IsChainValid);

    public static readonly DirectProperty<TrustStorePanel, Interaction<string, string?>?>
        OpenFileDialogProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, Interaction<string, string?>?>(
                nameof(OpenFileDialog),
                o => o.OpenFileDialog,
                (o, v) => o.OpenFileDialog = v);

    public static readonly DirectProperty<TrustStorePanel, ShadUI.DialogManager?>
        DialogManagerProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, ShadUI.DialogManager?>(
                nameof(DialogManager),
                o => o.DialogManager,
                (o, v) => o.DialogManager = v);

    public static readonly DirectProperty<TrustStorePanel, OpenCertificateService?>
        OpenCertificateServiceProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, OpenCertificateService?>(
                nameof(OpenCertificateService),
                o => o.OpenCertificateService,
                (o, v) => o.OpenCertificateService = v);

    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private ShadUI.DialogManager? _dialogManager;
    private ObservableCollection<string> _errors = new();
    private bool _isChainValid = true;
    private OpenCertificateService? _openCertificateService;
    private Interaction<string, string?>? _openFileDialog;
    private bool _useSystemStore = true;
    static TrustStorePanel() => AffectsArrange<TrustStorePanel>(CertificateChainProperty);

    public TrustStorePanel()
    {
        InitializeComponent();

        this.WhenAnyValue(
                x => x.TrustedCertificates,
                x => x.CertificateChain,
                x => x.UseSystemStore,
                (_, _, _) => "changed")
            .Subscribe(async void (_) => await UpdateValidity());

        DataContext = this;
    }


    public ShadUI.DialogManager? DialogManager
    {
        get => _dialogManager;
        set => SetAndRaise(DialogManagerProperty, ref _dialogManager, value);
    }

    public OpenCertificateService? OpenCertificateService
    {
        get => _openCertificateService;
        set => SetAndRaise(OpenCertificateServiceProperty, ref _openCertificateService, value);
    }


    public ObservableCollection<string> Errors
    {
        get => _errors;
        set => SetAndRaise(ErrorsProperty, ref _errors, value);
    }

    public bool UseSystemStore
    {
        get => _useSystemStore;
        set => SetAndRaise(UseSystemStoreProperty, ref _useSystemStore, value);
    }

    public ObservableCollection<X509Certificate2> TrustedCertificates { get; set; } = new();

    public ObservableCollection<X509Certificate2> CertificateChain
    {
        get => _certificateChain;
        set => SetAndRaise(CertificateChainProperty, ref _certificateChain, value);
    }


    public bool IsChainValid
    {
        get => _isChainValid;
        set => SetAndRaise(IsChainValidProperty, ref _isChainValid, value);
    }

    public Interaction<string, string?>? OpenFileDialog
    {
        get => _openFileDialog;
        set => SetAndRaise(OpenFileDialogProperty, ref _openFileDialog, value);
    }

    [RelayCommand]
    public Task RemoveFromTrustedStore(X509Certificate2 certificate)
    {
        TrustedCertificates.Remove(certificate);
        return Task.CompletedTask;
    }


    private async Task UpdateValidity()
    {
        if (!CertificateChain.Any())
        {
            return;
        }

        var validityService = new ChainValidator();
        var result = await validityService.Validate(CertificateChain, TrustedCertificates, _useSystemStore);
        Errors.Clear();
        Errors.AddRange(result);
        IsChainValid = result.Count == 0;
    }

    [RelayCommand]
    public async Task AddRootCertificate()
    {
        // try
        {
            var fileDialogResult = await this.OpenFileDialogAsync("Root certificates");

            foreach (var file in fileDialogResult)
            {
                var result = await _openCertificateService!.OpenFile(file);
                if (result.Success == DialogResult.OperationResult.Success)
                {
                    TrustedCertificates.Add(result.Certificates);
                }
            }
        }
        // catch (Exception e)
        // {
            // _dialogManager?.CreateDialog("Error loading file", e.Message)
                // .Dismissible()
                // .Show();
        // }
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e) =>
        FlyoutBase.ShowAttachedFlyout(BorderWarning);
}
