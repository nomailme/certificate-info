using System;
using System.ComponentModel;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.Services;

/// <summary>
/// A global error handler that displays a message box when unhandled errors occur.
/// </summary>
public class GlobalErrorHandler : IObserver<Exception>
{
    private DialogManager? _dialogManager;

    public static GlobalErrorHandler Instance => _instance ??= new GlobalErrorHandler();
    private static GlobalErrorHandler? _instance;

    /// <summary>
    /// Sets the global error handler to display all errors.
    /// We set dependencies later because we want to initialize the ViewModelLocator in parallel to the View,
    /// and DefaultExceptionHandler must be set before creating the view.
    /// </summary>
    public static void BeginInit() => RxApp.DefaultExceptionHandler = Instance;

    /// <summary>
    /// Sets the dependencies for handling and displaying errors.
    /// </summary>
    /// <param name="dialogManager">The service used to display dialogs.</param>
    /// <param name="ownerVm">The ViewModel of the View in which to display error messages.</param>
    public static void EndInit(DialogManager dialogManager, INotifyPropertyChanged? ownerVm)
    {
        Instance._dialogManager = dialogManager;
        Instance.OwnerVm = ownerVm;
    }

    /// <summary>
    /// Gets or sets the ViewModel of the View in which to display error messages.
    /// </summary>
    public INotifyPropertyChanged? OwnerVm { get; set; }

    /// <inheritdoc />
    public async void OnNext(Exception error) => ShowError(error);

    /// <inheritdoc />
    public void OnError(Exception error)
    {
    }

    /// <inheritdoc />
    public void OnCompleted()
    {
    }

    private void ShowError(Exception error)
    {
        if (_dialogManager != null && OwnerVm != null)
        {
            _dialogManager.CreateDialog("Application Error", error.ToString()).Show();
        }
    }
}
