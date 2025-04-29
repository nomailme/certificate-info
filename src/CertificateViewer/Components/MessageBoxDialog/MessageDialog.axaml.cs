using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CertificateViewer.Components.MessageBoxDialog;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    public MessageDialog(MessageBoxVm viewModel)
    {
        InitializeComponent();
        DataContext = ViewModel = viewModel;
    }

    public MessageBoxVm ViewModel { get; set; } = new();

    public static MessageDialog Info(string title, string message, string okButtonText = "OK")
    {
        var vm = new MessageBoxVm
        {
            Title = title,
            Message = message,
            Icon = $"{Convert.ToChar(58493)}",
            IconColor = new SolidColorBrush(Color.Parse("#2563EB")),
            ShowCancel = false,
            OkButtonText = okButtonText
        };
        return Create(vm);
    }

    private static MessageDialog Create(MessageBoxVm viewModel) => new(viewModel);

    public static MessageDialog Warning(string title, string message, bool showCancel = true) =>
        Create(new MessageBoxVm
        {
            Title = title,
            Message = message,
            Icon = $"{Convert.ToChar(58490)}", // warning
            IconColor = new SolidColorBrush(Color.Parse("#F59E0B")),
            ShowCancel = showCancel,
            OkButtonText = "OK"
        });

    public static MessageDialog Error(string title, string message) =>
        Create(new MessageBoxVm
        {
            Title = title,
            Message = message,
            Icon = $"{Convert.ToChar(58497)}", // error
            IconColor = new SolidColorBrush(Color.Parse("#DC2626")),
            ShowCancel = false,
            OkButtonText = "OK"
        });
    // ViewModel = new MessageBoxVm
    // {
    //     Title = title,
    //     Message = message,
    //     Icon = Application.Current.FindResource("error_circle_regular") as string,
    //     IconColor = new SolidColorBrush(Color.Parse("#DC2626")),
    //     ShowCancel = false,
    //     OkButtonText = "OK"
    // }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close(false);

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close(false);

    private void OnOkClick(object sender, RoutedEventArgs e) => Close(true);
}
