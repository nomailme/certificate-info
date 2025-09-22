using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Components.MainWindow2;
using CertificateViewer.Services;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using OpenUrlViewModel2 = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlViewModel2;

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
        collection.AddTransient<OpenUrlViewModel2>();
        collection.AddTransient<PasswordDialogViewModel>();
        collection.AddSingleton<MainWindow2ViewModel>();
        collection.AddSingleton<ThemeWatcher>(_ => new ThemeWatcher(Current!));

        var services = collection.BuildServiceProvider();

        services.RegisterDialogs();

        var vm = DataContext = services.GetRequiredService<MainWindow2ViewModel>();
        var mainWindow = new MainWindow2 { DataContext = vm };
        desktop.MainWindow = mainWindow;

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}
