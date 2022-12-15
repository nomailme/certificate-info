using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CertificateViewer.Dialogs;
using ReactiveUI;

namespace CertificateViewer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<MainWindowVm>
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WhenActivated(
                d =>
                {
                    d(ViewModel!.ShowOpenFileDialog.RegisterHandler(DoShowOpenFileDialog));
                    d(ViewModel!.ShowMessageBox.RegisterHandler(DoShowMessageBox));
                    d(ViewModel!.ShowOpenUrlDialog.RegisterHandler(DoShowOpenUrlDialog));
                });
        }

        private async Task DoShowOpenFileDialog(InteractionContext<string, string?> arg)
        {
            var dialog = new OpenFileDialog { AllowMultiple = false };
            var result = await dialog.ShowAsync(this);
            arg.SetOutput(result?.Single());

        }

        private async Task DoShowOpenUrlDialog(InteractionContext<string, string?> arg)
        {
            var dialog = new OpenUrlWindow();
            await dialog.ShowDialog(this);
            arg.SetOutput(dialog.DialogResult ? dialog.Url : null);

        }

        private async Task DoShowMessageBox(InteractionContext<(string Title, string Message), MessageBox.MessageBoxResult> arg)
        {
            var result = await MessageBox.Show(this, arg.Input.Message, arg.Input.Title, MessageBox.MessageBoxButtons.Ok);
            arg.SetOutput(result);
        }
    }
}
