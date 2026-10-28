using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Components.MainWindow;
using CertificateViewer.Services;
using CertificateViewer.Services.ErrorHandling;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public class App : Application
{
    private IErrorHandler? _errorHandler;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;
    }
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // The handler is resolved from DI in OnFrameworkInitializationCompleted; if an
        // unhandled exception fires before that point, there is nothing to show yet.
        if (_errorHandler is not null)
        {
            _ = _errorHandler.HandleAsync(e.Exception);
        }

        e.Handled = true;
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        DisableAvaloniaDataAnnotationValidation();

        var collection = new ServiceCollection();
        collection.AddSingleton<ViewModelFactory>();
        collection.RegisterDialogViews();
        collection.AddTransient<OpenUrlVm>();
        collection.AddTransient<PasswordDialogViewModel>();
        collection.AddSingleton<ICertificateChainValidator, ChainValidator>();
        collection.AddSingleton<IErrorHandler, GlobalErrorHandler>();
        collection.AddSingleton<DerImporter>();
        collection.AddSingleton<PemImporter>();
        collection.AddSingleton<PfxImporter>();
        collection.AddSingleton<RemoteServerImporter>();
        collection.AddSingleton<ICertificateLoader<string>>(sp => sp.GetRequiredService<RemoteServerImporter>());
        collection.AddSingleton<OpenCertificateService>();
        collection.AddSingleton<CertificateSourceLoader>();
        collection.AddSingleton<MainWindowVm>();
        collection.AddSingleton<ThemeWatcher>(_ => new ThemeWatcher(Current!));

        var services = collection.BuildServiceProvider();

        _errorHandler = services.GetRequiredService<IErrorHandler>();

        // Composition root: the child view models are constructed here with their dependencies
        // injected, then handed to MainWindowVm (which wires their view-state to the panel state).
        var openCertificateService = services.GetRequiredService<OpenCertificateService>();
        var certificateSourceLoader = services.GetRequiredService<CertificateSourceLoader>();
        var chainValidator = services.GetRequiredService<ICertificateChainValidator>();
        var trustStoreViewModel = new TrustStoreViewModel(openCertificateService, chainValidator, _errorHandler);
        var certificateChainViewModel = new CertificateChainViewModel();
        var certificateDetailsViewModel = new CertificateDetailsViewModel();

        var dialogManager = services.GetRequiredService<ShadUI.DialogManager>();
        var viewModelFactory = services.GetRequiredService<ViewModelFactory>();
        var themeWatcher = services.GetRequiredService<ThemeWatcher>();
        var vm = new MainWindowVm(
            dialogManager,
            viewModelFactory,
            themeWatcher,
            _errorHandler,
            openCertificateService,
            certificateSourceLoader,
            trustStoreViewModel,
            certificateChainViewModel,
            certificateDetailsViewModel);
        var mainWindow = new MainWindow
        {
            DataContext = vm
        };
        // The dialog host is a view concern: the manager that CreateDialog renders into is wired
        // here from the DI singleton (the view model stays service-free).
        mainWindow.DialogHost.Manager = dialogManager;
        desktop.MainWindow = mainWindow;

        var args = desktop.Args;


        if (args?.Length == 1)
        {
            mainWindow.WhenActivated(d =>
                {
                    _ = vm.LoadFileAsync(args.Single());
                    Disposable.Create(() => { }).DisposeWith(d);
                }
            );
        }

        base.OnFrameworkInitializationCompleted();
    }

    [RequiresUnreferencedCode("Calls Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators")]
    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
