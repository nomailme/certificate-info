using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CertificateViewer.Components.DialogManager;
using CertificateViewer.Components.MessageBoxDialog;
using CertificateViewer.Controls.Dialogs;
using CertificateViewer.Logic;
using CertificateViewer.Services;
using DynamicData;
using ReactiveUI;

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

    // public static readonly DirectProperty<TrustStorePanel, Interaction<DialogViewModel, MessageBox.MessageBoxResult>?>
    //     MessageBoxDialogProperty =
    //         AvaloniaProperty
    //             .RegisterDirect<TrustStorePanel, Interaction<DialogViewModel, MessageBox.MessageBoxResult>?>(
    //                 nameof(MessageBoxDialog),
    //                 o => o.MessageBoxDialog,
    //                 (o, v) => o.MessageBoxDialog = v);

    public static readonly DirectProperty<TrustStorePanel, Interaction<string, string?>?>
        OpenFileDialogProperty =
            AvaloniaProperty.RegisterDirect<TrustStorePanel, Interaction<string, string?>?>(
                nameof(OpenFileDialog),
                o => o.OpenFileDialog,
                (o, v) => o.OpenFileDialog = v);


    private readonly OpenCertificateService _certificateService = new();
    private ObservableCollection<X509Certificate2> _certificateChain = new();
    private ObservableCollection<string> _errors = new();
    private bool _isChainValid = true;
    // private Interaction<DialogViewModel, MessageBox.MessageBoxResult>? _messageBoxDialog;
    private Interaction<string, string?>? _openFileDialog;
    private bool _useSystemStore = true;

    public TrustStorePanel()
    {
        InitializeComponent();

        var canAddCertificate = this.WhenAnyValue(x => x.UseSystemStore).Select(x=>!x);
        AddToTrustStoreCommand = ReactiveCommand.CreateFromTask(AddRootCertificate, canAddCertificate);

        var trustedCertificates = this.WhenAnyValue(
            x => x.TrustedCertificates,
            x => x.CertificateChain,
            x => x.UseSystemStore,
            (p1, p2, p3) => "changed");

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


    // public Interaction<DialogViewModel, MessageBox.MessageBoxResult>? MessageBoxDialog
    // {
    //     get => _messageBoxDialog;
    //     set => SetAndRaise(MessageBoxDialogProperty, ref _messageBoxDialog, value);
    // }

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


    private void UpdateValidity()
    {
        if (CertificateChain is null)
        {
            return;
        }

        if (!CertificateChain.Any())
        {
            return;
        }

        List<string> errors = new();
        var validityService = new ChainValidator();
        var result = validityService.Validate(CertificateChain, TrustedCertificates, _useSystemStore, out errors);
        Errors.Clear();
        Errors.AddRange(errors);
        IsChainValid = result;
    }


    private async Task AddRootCertificate()
    {
        try
        {
            var fileDialogResult = await this.OpenFileDialogAsync("Root certificates", selectMany: true);

            if (fileDialogResult is null)
            {
                return;
            }

            foreach (var file in fileDialogResult)
            {
                var result = await _certificateService.OpenFile(file);
                if (result.Success == DialogResult.OperationResult.Success)
                {
                    TrustedCertificates.Add(result.Certificates);
                }

            }
        }
        catch (Exception e)
        {

            MessageDialog.Error("Error loading file", e.Message).Show();
        }
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e) =>
        FlyoutBase.ShowAttachedFlyout(BorderWarning);
}
