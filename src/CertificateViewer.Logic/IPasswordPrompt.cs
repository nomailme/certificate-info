namespace CertificateViewer.Logic;

/// <summary>
///     Abstraction for prompting the user for a password. Implemented by the UI layer
///     (which shows a dialog) and consumed by the domain service. Returns null when the
///     user cancels the prompt.
/// </summary>
public interface IPasswordPrompt
{
    /// <summary>
    ///     Shows a password prompt. Returns the entered password, or null if the user cancelled.
    /// </summary>
    Task<string?> PromptAsync();
}
