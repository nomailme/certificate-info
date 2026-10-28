using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ShadUI;

namespace CertificateViewer.ViewModels;

/// <summary>
///     Base for dialog view models: the shared Submit/Cancel commands and the explicit
///     per-property validation support (no reflection, no DataAnnotations machinery, so it is
///     trim-safe under Native AOT). Derived classes implement <see cref="ValidateAll" /> with
///     their own property checks and rely on the base commands to close the dialog.
/// </summary>
public abstract partial class DialogViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    protected DialogViewModelBase(ShadUI.DialogManager dialogManager) => DialogManager = dialogManager;

    /// <summary>Gets the dialog manager used to close the dialog.</summary>
    protected ShadUI.DialogManager DialogManager { get; }

    public bool HasErrors => _errors.Count != 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out var errors))
        {
            yield break;
        }

        foreach (var error in errors)
        {
            yield return error;
        }
    }

    /// <summary>Records a validation error for the property (no-op when the error is already recorded).</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="error">The error message.</param>
    protected void AddError(string propertyName, string error)
    {
        if (!_errors.TryGetValue(propertyName, out var value))
        {
            value = new List<string>();
            _errors[propertyName] = value;
        }

        if (value.Contains(error))
        {
            return;
        }

        value.Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        SubmitCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Clears any recorded errors for the property.</summary>
    /// <param name="propertyName">The property name.</param>
    protected void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            SubmitCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSubmit() => !HasErrors;

    /// <summary>Runs all property validation and closes the dialog on success; does nothing while
    ///     errors remain.</summary>
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        ValidateAll();
        if (HasErrors)
        {
            return;
        }

        DialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    /// <summary>Closes the dialog without reporting success.</summary>
    [RelayCommand]
    private void Cancel() => DialogManager.Close(this);

    /// <summary>Runs each derived class's explicit validation over its properties.</summary>
    protected abstract void ValidateAll();
}
