using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CertificateViewer.Dialogs;

public partial class OpenUrlWindow : Window, INotifyPropertyChanged
{
    private string? url = "https://";

    public OpenUrlWindow() => InitializeComponent();

    public string? Url
    {
        get => url;
        set
        {
            url = value;
            OnPropertyChanged();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;


    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UrlAddressTextBox.CaretIndex = UrlAddressTextBox.Text.Length;
    }


    private void cancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValid(this))
        {
            return;
        }
        DialogResult = true;
    }

    private bool IsValid(DependencyObject? node)
    {
        // Check if dependency object was passed
        if (node != null)
        {
            var isValid = !Validation.GetHasError(node);
            if (!isValid)
            {
                if (node is IInputElement element)
                {
                    Keyboard.Focus(element);
                }
                return false;
            }
        }

        // If this dependency object is valid, check all child dependency objects
        return LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>().All(IsValid);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
