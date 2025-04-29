using Avalonia.Media;
using CertificateViewer.ViewModels;
using ReactiveUI;

namespace CertificateViewer.Components.MessageBoxDialog;

public class MessageBoxVm : BaseViewModel
{
    private string _title = "This is a title";
    private string _message = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public string Icon { get; set; }
    public IBrush IconColor { get; set; }
    public bool ShowCancel { get; set; }
    public string OkButtonText { get; set; }
}
