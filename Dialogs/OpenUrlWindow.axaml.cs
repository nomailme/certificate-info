using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CertificateViewer.Dialogs;

public partial class OpenUrlWindow : Window
{
    private string? url = "https://";

    public OpenUrlWindow() => InitializeComponent();

    public bool DialogResult { get; set; }

    public string? Url
    {
        get => url;
        set => url = value;
    }

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
        // if (!IsValid(this))
        // {
            // return;
        // }
        DialogResult = true;
        Close();
    }

    // private bool IsValid(DependencyObject? node)
    // {
    //     // Check if dependency object was passed
    //     if (node != null)
    //     {
    //         var isValid = !Validation.GetHasError(node);
    //         if (!isValid)
    //         {
    //             if (node is IInputElement element)
    //             {
    //                 Keyboard.Focus(element);
    //             }
    //             return false;
    //         }
    //     }
    //
    //     // If this dependency object is valid, check all child dependency objects
    //     return LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>().All(IsValid);
    // }
}
