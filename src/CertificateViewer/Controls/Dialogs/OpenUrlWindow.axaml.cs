using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CertificateViewer.Controls.Dialogs;

public partial class OpenUrlWindow : Window
{
    public OpenUrlWindow() => InitializeComponent();

    public bool DialogResult { get; set; }

    public string? Url { get; set; } = "https://";

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UrlAddressTextBox.CaretIndex = UrlAddressTextBox.Text.Length;
    }

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;

        Close();
    }

    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
