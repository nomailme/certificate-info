using System.Windows;
using System.Windows.Media;
using CertificateViewerPlayground.Themes.Extensions;

namespace CertificateViewerPlayground
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (TryFindResource("AccentColorBrush") is SolidColorBrush accentBrush) accentBrush.Color.CreateAccentColors();
        }
    }
}