using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CertificateViewerPlayground;

public partial class OpenUrlWindow : Window, INotifyPropertyChanged
{
    private string? url;
    public OpenUrlWindow()
    {
        InitializeComponent();
    }

    public string? Url
    {
        get => url;
        set
        {
            url = value;
            OnPropertyChanged();
        }
    }


    private void cancelButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;

    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValid(this))
        {
            return;
        }
        DialogResult = true;
    }

    // Validate all dependency objects in a window
    private bool IsValid(DependencyObject? node)
    {
        // Check if dependency object was passed
        if (node != null)
        {
            // Check if dependency object is valid.
            // NOTE: Validation.GetHasError works for controls that have validation rules attached
            var isValid = !Validation.GetHasError(node);
            if (!isValid)
            {
                // If the dependency object is invalid, and it can receive the focus,
                // set the focus
                if (node is IInputElement)
                {
                    Keyboard.Focus((IInputElement)node);
                }
                return false;
            }
        }

        // If this dependency object is valid, check all child dependency objects
        return LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>().All(IsValid);

        // All dependency objects are valid
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}