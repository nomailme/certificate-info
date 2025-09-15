using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace CertificateViewer.Components.MainWindow2;

public partial class MainWindow2 : ReactiveWindow<MainWindow2ViewModel>
{
    public MainWindow2()
    {
        InitializeComponent();
        this.WhenActivated(
            d =>
            {
                d(ViewModel!.ShowOpenFileDialog.RegisterHandler(DoShowOpenFileDialog));
                d(ViewModel!.ShowOpenUrlDialog.RegisterHandler(DoShowOpenUrlDialog));
            });
        SetVersion();

    }

    private void SetVersion()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.
            InformationalVersion;

        Title = $"Certificate Viewer {version}";

    }

    private async Task DoShowOpenFileDialog(IInteractionContext<string, string> arg)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false, Title = "Select a file"
        });

        if (files.Any() == false)
        {
            arg.SetOutput(string.Empty);
            return;
        }

        arg.SetOutput(files[0].Path.AbsolutePath);
    }

    private async Task DoShowOpenUrlDialog(IInteractionContext<string, string?> arg)
    {
        var dialog = new OpenUrlDialog.OpenUrlDialog();
        await dialog.ShowDialog(this);

        if (dialog.Success)
        {
            var result = dialog.EnteredUrl;
            arg.SetOutput(result);
            return;
        }

        arg.SetOutput(null);
    }
}
