using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CertificateViewer.Components.OpenUrlDialog;

public partial class OpenUrlDialog : Window
{
    public OpenUrlDialog() => InitializeComponent();

    public bool Success { get; set; }

    public string EnteredUrl { get; private set; } = "https://";

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var urlInput = this.FindControl<TextBox>("UrlInput");
        if (urlInput != null)
        {
            urlInput.Focus();
            urlInput.Text = "https://";
            urlInput.CaretIndex = 999;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter)
        {
            OnFetchClick(null, null);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close(null);

    private void OnFetchClick(object sender, RoutedEventArgs e)
    {
        var urlInput = this.FindControl<TextBox>("UrlInput");
        if (urlInput != null && !string.IsNullOrWhiteSpace(urlInput.Text))
        {
            EnteredUrl = urlInput.Text;
            Success = true;
            Close(EnteredUrl);
        }
    }
}
