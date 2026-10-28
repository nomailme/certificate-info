using System;
using System.Threading.Tasks;
using CertificateViewer.Services.ErrorHandling;
using ShadUI;

namespace CertificateViewer.Services;

/// <summary>
///     A global error handler that displays an error dialog when unhandled errors occur.
/// </summary>
public class GlobalErrorHandler : IErrorHandler
{
    private readonly DialogManager _dialogManager;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GlobalErrorHandler" /> class.
    /// </summary>
    /// <param name="dialogManager">The service used to display dialogs.</param>
    public GlobalErrorHandler(DialogManager dialogManager)
    {
        _dialogManager = dialogManager;
    }

    /// <inheritdoc />
    public Task HandleAsync(Exception exception, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (_dialogManager != null)
        {
            _dialogManager.CreateDialog("Application Error", exception.ToString()).Show();
        }

        return Task.CompletedTask;
    }
}
