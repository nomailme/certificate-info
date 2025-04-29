using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CertificateViewer.Components.MainWindow2;

namespace CertificateViewer;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow2();

        }

        base.OnFrameworkInitializationCompleted();
    }
}
