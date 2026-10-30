using CertificateViewer.Components.Dialogs.PasswordBox;
using ShadUI;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Validation tests for the password dialog view model, mirroring the OpenUrlViewModel suite:
///     an empty password must block submission, a non-empty one must allow it, and the base
///     class's Submit/Cancel commands must close the dialog through the concrete-type callback
///     (the same bug the OpenUrl tests guard against).
/// </summary>
public class PasswordDialogViewModelTests
{
    private static DialogManager CreateManager()
    {
        var manager = new DialogManager();
        manager.Register<PasswordDialogContent, PasswordDialogViewModel>();
        return manager;
    }

    [Fact]
    public void Submit_EmptyPassword_ReportsError()
    {
        var vm = new PasswordDialogViewModel(CreateManager());

        vm.SubmitCommand.Execute(null);

        Assert.True(vm.HasErrors);
        Assert.NotEmpty(vm.GetErrors(nameof(vm.Password)));
    }

    [Fact]
    public void Submit_WhitespacePassword_ReportsError()
    {
        var vm = new PasswordDialogViewModel(CreateManager());
        vm.Password = "   ";

        vm.SubmitCommand.Execute(null);

        Assert.True(vm.HasErrors);
    }

    [Fact]
    public void Submit_Password_ReportsNoError()
    {
        var vm = new PasswordDialogViewModel(CreateManager());
        vm.Password = "secret";

        vm.SubmitCommand.Execute(null);

        Assert.False(vm.HasErrors);
    }

    [Fact]
    public void Submit_ValidPassword_FiresSuccessCallback()
    {
        string? passwordSeen = null;
        var canceled = false;

        TestPlatform.RunOnUi(() =>
        {
            var manager = CreateManager();
            var vm = new PasswordDialogViewModel(manager);
            manager.CreateDialog(vm)
                .WithSuccessCallback(m => passwordSeen = m.Password)
                .WithCancelCallback(() => canceled = true)
                .Show();

            vm.Password = "secret";
            vm.SubmitCommand.Execute(null);
            TestPlatform.Pump();
        });

        Assert.Equal("secret", passwordSeen);
        Assert.False(canceled);
    }

    [Fact]
    public void Cancel_FiresCancelCallback()
    {
        string? passwordSeen = null;
        var canceled = false;

        TestPlatform.RunOnUi(() =>
        {
            var manager = CreateManager();
            var vm = new PasswordDialogViewModel(manager);
            manager.CreateDialog(vm)
                .WithSuccessCallback(m => passwordSeen = m.Password)
                .WithCancelCallback(() => canceled = true)
                .Show();

            vm.CancelCommand.Execute(null);
            TestPlatform.Pump();
        });

        Assert.Null(passwordSeen);
        Assert.True(canceled);
    }
}
