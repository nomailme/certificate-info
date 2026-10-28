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
        collection.AddSingleton<IErrorHandler, GlobalErrorHandler>();
        collection.AddSingleton<MainWindowVm>();
        collection.AddSingleton<ThemeWatcher>(_ => new ThemeWatcher(Current!));

        var services = collection.BuildServiceProvider();

        _errorHandler = services.GetRequiredService<IErrorHandler>();

        var vm = services.GetRequiredService<MainWindowVm>();
        var mainWindow = new MainWindow
        {
            DataContext = vm
        };
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
