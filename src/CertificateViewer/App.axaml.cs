using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Components.MainWindow;
using CertificateViewer.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {


        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        DisableAvaloniaDataAnnotationValidation();

        var collection = new ServiceCollection();
        collection.AddSingleton<ViewModelFactory>();
        collection.AddSingleton<DialogManager>();
        collection.AddTransient<OpenUrlVm>();
        collection.AddTransient<PasswordDialogViewModel>();
        collection.AddSingleton<MainWindowVm>();
        collection.AddSingleton<ThemeWatcher>(_ => new ThemeWatcher(Current!));

        var services = collection.BuildServiceProvider();

        services.RegisterDialogs();

        var vm = services.GetRequiredService<MainWindowVm>();
        var mainWindow = new MainWindow
        {
            DataContext = vm
        };
        desktop.MainWindow = mainWindow;

        var args = desktop.Args;


        if (args?.Length == 1)
        {
            mainWindow.WhenActivated(async void (d) =>
                {
                    await vm.LoadFileAsync(args.Single());
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
