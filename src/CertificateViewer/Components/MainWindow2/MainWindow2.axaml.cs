using System.Reflection;
using ShadUI;

namespace CertificateViewer.Components.MainWindow2;

public partial class MainWindow2 : Window
{
    public MainWindow2()
    {
        InitializeComponent();
        SetVersion();
    }

    private void SetVersion()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Title = $"Certificate Viewer {version}";
    }
}
