using System.Windows;
using System.Windows.Input;

namespace CertificateViewerPlayground;

public partial class OpenUrlWindow : Window
{
    public OpenUrlWindow()
    {
        InitializeComponent();
    }

    public string Text { get; set; }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Text = InputBox.Text;
        DialogResult = true;

        Close();
    }
}
