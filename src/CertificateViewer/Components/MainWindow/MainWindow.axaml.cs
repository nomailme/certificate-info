using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ShadUI;

namespace CertificateViewer.Components.MainWindow;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetVersion();
        AddHandler(DragDrop.DropEvent, DragDropHandler);
    }
    private async Task DragDropHandler(object? sender, DragEventArgs e)
    {
        var vm = DataContext as MainWindowVm;
        try
        {

            if (e.Data.GetFiles() is { } fileNames && vm is not null)
            {
                foreach (var file in fileNames)
                {
                    var path = file.TryGetLocalPath();
                    if (path is not null)
                    {
                        await vm.LoadFileAsync(path);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                vm?.DialogManager.CreateDialog("Error opening file", ex.Message).Show();
            });

        }


    }



    private void SetVersion()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Title = $"Certificate Viewer {version}";
    }
}
