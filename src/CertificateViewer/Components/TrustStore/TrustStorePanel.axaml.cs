using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Logic;
using CertificateViewer.Services;
using DynamicData;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.TrustStore;

public partial class TrustStorePanel : UserControl
{
    private ShadUI.DialogManager _dialogManager;

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

    public static readonly DirectProperty<TrustStorePanel, ShadUI.DialogManager>
        DialogManagerProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, ShadUI.DialogManager>(
                nameof(DialogManager),
                o => o.DialogManager,
                (o, v) => o.DialogManager = v);

    public static readonly DirectProperty<TrustStorePanel, OpenCertificateService>
        OpenCertificateServiceProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, OpenCertificateService>(
                nameof(OpenCertificateService),
                o => o.OpenCertificateService,
                (o, v) => o.OpenCertificateService = v);


    public ShadUI.DialogManager DialogManager
    {
        get => _dialogManager;
        set => SetAndRaise(DialogManagerProperty, ref _dialogManager, value);
    }

    public OpenCertificateService OpenCertificateService
    {
        get => _openCertificateService;
        set => SetAndRaise(OpenCertificateServiceProperty, ref _openCertificateService, value);
    }

    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private ObservableCollection<string> _errors = new();
    private bool _isChainValid = true;
    private Interaction<string, string?>? _openFileDialog;
    private bool _useSystemStore = true;
    private OpenCertificateService _openCertificateService;

    public TrustStorePanel()
    {
        InitializeComponent();

        var canAddCertificate = this.WhenAnyValue(x => x.UseSystemStore).Select(x=>!x);
        AddToTrustStoreCommand = ReactiveCommand.CreateFromTask(AddRootCertificate, canAddCertificate);

        var trustedCertificates = this.WhenAnyValue(
            x => x.TrustedCertificates,
            x => x.CertificateChain,
            x => x.UseSystemStore,
            (_, _, _) => "changed");

        trustedCertificates.Subscribe(_ => UpdateValidity());
        RemoveFromTrustStoreCommand =
            ReactiveCommand.CreateFromTask<X509Certificate2>(RemoveCertificateFromTrustedStore);
        DataContext = this;

    }
    static TrustStorePanel() => AffectsArrange<TrustStorePanel>(CertificateChainProperty);


    public ReactiveCommand<X509Certificate2, Unit> RemoveFromTrustStoreCommand { get; set; }

    public ObservableCollection<string> Errors
    {
        get => _errors;
        set => SetAndRaise(ErrorsProperty, ref _errors, value);
    }

    public bool UseSystemStore
    {
        get => _useSystemStore;
        set
        {
            SetAndRaise(UseSystemStoreProperty, ref _useSystemStore, value);
            UpdateValidity();
        }
    }

    public ObservableCollection<X509Certificate2> TrustedCertificates { get; set; } = new();

    public ReactiveCommand<Unit, Unit> AddToTrustStoreCommand { get; set; }

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

    private Task RemoveCertificateFromTrustedStore(X509Certificate2 certificate)
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


    private async Task AddRootCertificate()
    {
        try
        {
            var fileDialogResult = await this.OpenFileDialogAsync("Root certificates", selectMany: true);

            foreach (var file in fileDialogResult)
            {
                var result = await _openCertificateService.OpenFile(file);
                if (result.Success == DialogResult.OperationResult.Success)
                {
                    TrustedCertificates.Add(result.Certificates);
                }

            }
        }
        catch (Exception e)
        {
            _dialogManager.CreateDialog("Error loading file", e.Message)
                .Dismissible()
                .Show();
        }
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e) =>
        FlyoutBase.ShowAttachedFlyout(BorderWarning);
}
