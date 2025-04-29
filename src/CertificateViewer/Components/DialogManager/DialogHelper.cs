using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CertificateViewer.Components.MessageBoxDialog;

namespace CertificateViewer.Components.DialogManager;

public static class DialogHelper
{
    /// <summary>
    ///     Shows an open file dialog for a registered context, most likely a ViewModel
    /// </summary>
    /// <param name="context">The context</param>
    /// <param name="title">The dialog title or a default is null</param>
    /// <param name="selectMany">Is selecting many files allowed?</param>
    /// <returns>An array of file names</returns>
    /// <exception cref="ArgumentNullException">if context was null</exception>
    public static async Task<IEnumerable<string>?> OpenFileDialogAsync(this object? context, string? title = null,
        bool selectMany = true)
    {
        ArgumentNullException.ThrowIfNull(context);

        // lookup the TopLevel for the context
        var topLevel = DialogManager.GetTopLevelForContext(context);

        if (topLevel != null)
        {
            // Open the file dialog
            var storageFiles = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions { AllowMultiple = selectMany, Title = title ?? "Select any file(s)" });

            if (storageFiles.Count == 0)
            {
                return null;
            }

            // return the result
            return storageFiles.Select(s => s.TryGetLocalPath());
        }

        return null;
    }

    public static async Task<string?> ShowInfoMessage(this object? context, string title, string message, string okButtonText = "OK")
    {
        ArgumentNullException.ThrowIfNull(context);

        // lookup the TopLevel for the context
        var topLevel = DialogManager.GetTopLevelForContext(context);

            //if (topLevel != null)
        {
            // Open the file dialog
            await MessageDialog.Info(title, message, okButtonText).ShowDialog((Window)context);
        }

        return null;
    }

    public static async Task<string?> ShowErrorMessage(this object? context, string title, string message, string okButtonText = "OK")
    {
        ArgumentNullException.ThrowIfNull(context);

        // lookup the TopLevel for the context
        var topLevel = DialogManager.GetTopLevelForContext(context);

            if (topLevel != null)
        {
            // Open the file dialog
            await MessageDialog.Error(title, message).ShowDialog((Window)topLevel);
        }

        return null;
    }
}
