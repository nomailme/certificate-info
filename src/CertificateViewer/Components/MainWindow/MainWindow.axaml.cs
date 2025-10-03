using System;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Components.MainWindow;

public partial class MainWindow : Window, IViewFor<MainWindowVm>
{

    public static readonly StyledProperty<MainWindowVm?> ViewModelProperty =
        AvaloniaProperty.Register<MainWindow, MainWindowVm?>(nameof(ViewModel));

    public MainWindow()
    {
        InitializeComponent();
        SetVersion();
        AddHandler(DragDrop.DropEvent, DragDropHandler);
        // RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ShowErrorDialog);
    }
    private void ShowErrorDialog(Exception obj)
        => ViewModel?.DialogManager.CreateDialog("Error", obj.Message).Show();

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (MainWindowVm?)value;
    }
    public MainWindowVm? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
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
