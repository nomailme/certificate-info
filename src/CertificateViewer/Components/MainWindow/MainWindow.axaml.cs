using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.MainWindow;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, DragDropHandler);
    }

    private async Task DragDropHandler(object? sender, DragEventArgs e)
    {
        var vm = DataContext as MainWindowVm;
        try
        {
            if (e.DataTransfer.TryGetFiles() is { } fileNames && vm is not null)
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
                _ = vm?.HandleFileOpenError(ex);
            });
        }
    }
}
