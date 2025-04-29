using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CertificateViewer.Controls.Dialogs;

public partial class PasswordBox : Window
{
    public PasswordBox() => InitializeComponent();

    public bool? DialogResult { get; set; }

    public string Password { get; set; } = string.Empty;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PasswordTextBox.Focus();
    }

    public static async Task<string?> ShowPasswordBoxAsync(Window? owner)
    {
        if (owner is null)
        {
            return string.Empty;
        }

        var passwordBox = new PasswordBox();
        await passwordBox.ShowDialog(owner);
        return passwordBox.DialogResult == true ? passwordBox.Password : null;
    }

    private void Ok_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Password = PasswordTextBox?.Text ?? string.Empty;
        Close();
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
