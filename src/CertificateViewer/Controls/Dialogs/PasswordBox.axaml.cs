using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CertificateViewer.Controls.Dialogs;

public partial class PasswordBox : Window
{
    public PasswordBox()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PasswordTextBox.Focus();
    }

    public bool? DialogResult { get; set; }

    public string Password { get; set; } = string.Empty;

    public static async Task<string?> ShowPasswordBoxAsync(Window owner)
    {
        var passwordBox = new PasswordBox();
        await passwordBox.ShowDialog(owner);
        if (passwordBox.DialogResult == true)
        {
            return passwordBox.Password;
        }
        return null;
    }

    private void Ok_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Password = PasswordTextBox.Text;
        Close();
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
