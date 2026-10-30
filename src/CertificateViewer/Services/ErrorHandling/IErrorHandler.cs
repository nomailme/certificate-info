using System;
using System.Threading.Tasks;

namespace CertificateViewer.Services.ErrorHandling;

/// <summary>
///     Handles errors raised anywhere in the application by surfacing them to the user.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    ///     Handles the given exception, typically by showing an error dialog.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="context">Optional context describing where the error occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(Exception exception, string? context = null);
}
