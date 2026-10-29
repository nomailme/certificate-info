using System;
using CertificateViewer.Components.Dialogs.OpenUrl;
using ShadUI;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Regression tests for the "enter a URL, nothing loads" bug.
///
///     The symptom: the user types a valid address into the Open URL dialog and presses Submit,
///     but no certificate chain ever loads. The view model was already validating the URL correctly
///     (the <see cref="OpenUrlVmTests" /> suite covered that), so the bug was one level down: the
///     dialog's success callback was being <em>silently dropped</em>.
///
///     Root cause: <c>DialogViewModelBase.Submit()</c> closed the dialog via
///     <c>DialogManager.Close(this, ...)</c>. Because the call was made from the base class, C#
///     inferred <c>Close&lt;TContext&gt;</c> with <c>TContext = DialogViewModelBase</c> (the static
///     type of <c>this</c>). But the app registers its success callback via
///     <c>manager.CreateDialog(openUrlViewModel).WithSuccessCallback(vm => Load(vm.Url))</c>, and
///     ShadUI keys its callbacks on the context type that <c>CreateDialog&lt;TContext&gt;</c>
///     infers — the <em>concrete</em> <c>OpenUrlVm</c>. ShadUI's <c>InvokeCallBacks</c> then looked
///     up the callback under the base type, found nothing, and dropped it. Validation passed, the
///     command ran, the dialog closed, and <c>Load(vm.Url)</c> never fired.
///
///     These tests wire the callback up exactly the way the app does (concrete <c>CreateDialog(vm)
///     .WithSuccessCallback(...)</c>) and assert that running the dialog's Submit command actually
///     fires it. Before the fix they fail (callback never fires); after the fix they pass. They run
///     on the UI thread because ShadUI's <c>CreateDialog</c> constructs the concrete control.
/// </summary>
public class OpenUrlDialogSubmitCallbackTests
{
    /// <summary>
    ///     The core regression: submit a valid URL through the dialog's own Submit command (the
    ///     same command the UI button/HotKey Enter run) and confirm the success callback the app
    ///     registered is invoked with the typed URL. This is the path that silently did nothing
    ///     before the fix.
    /// </summary>
    [Fact]
    public void Submit_ValidUrl_FiresAppSuccessCallbackWithTypedUrl()
    {
        string? submitted = null;

        TestPlatform.RunOnUi(() =>
        {
            var manager = new DialogManager();
            manager.Register<OpenUrlContent, OpenUrlVm>();
            var vm = new OpenUrlVm(manager);

            // Registered exactly like MainWindowVm.OpenUrl does.
            manager.CreateDialog(vm)
                .WithSuccessCallback(m => submitted = m.Url)
                .WithCancelCallback(() => { })
                .Show();

            vm.Url = "https://example.com";

            // This is the command the Submit button and the HotKey Enter run.
            vm.SubmitCommand.Execute(null);
        });

        Assert.Equal("https://example.com", submitted);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://sub.example.com:8443/path")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("https://1.2.3.4")]
    public void Submit_ValidUrl_SignalsSuccess(string url)
    {
        var fired = false;
        string? urlSeen = null;

        TestPlatform.RunOnUi(() =>
        {
            var manager = new DialogManager();
            manager.Register<OpenUrlContent, OpenUrlVm>();
            var vm = new OpenUrlVm(manager);
            manager.CreateDialog(vm)
                .WithSuccessCallback(m => { fired = true; urlSeen = m.Url; })
                .WithCancelCallback(() => { })
                .Show();

            vm.Url = url;
            vm.SubmitCommand.Execute(null);
        });

        Assert.True(fired, "submitting a valid URL must fire the app's success callback");
        Assert.Equal(url, urlSeen);
    }

    [Fact]
    public void Submit_InvalidUrl_DoesNotFireSuccessCallback()
    {
        var success = false;

        TestPlatform.RunOnUi(() =>
        {
            var manager = new DialogManager();
            manager.Register<OpenUrlContent, OpenUrlVm>();
            var vm = new OpenUrlVm(manager);
            manager.CreateDialog(vm)
                .WithSuccessCallback(_ => success = true)
                .WithCancelCallback(() => { })
                .Show();

            vm.Url = "https://"; // host-less: invalid
            vm.SubmitCommand.Execute(null);
            Assert.True(vm.HasErrors);
        });

        Assert.False(success, "an invalid URL must not fire the success callback");
    }

    [Fact]
    public void Cancel_FiresAppCancelCallback()
    {
        var cancelled = false;

        TestPlatform.RunOnUi(() =>
        {
            var manager = new DialogManager();
            manager.Register<OpenUrlContent, OpenUrlVm>();
            var vm = new OpenUrlVm(manager);
            manager.CreateDialog(vm)
                .WithSuccessCallback(_ => { })
                .WithCancelCallback(() => cancelled = true)
                .Show();

            // Cancel went through the same base-type Close bug as Submit, so it was also being dropped.
            vm.CancelCommand.Execute(null);
        });

        Assert.True(cancelled, "cancelling the dialog must fire the app's cancel callback");
    }
}
