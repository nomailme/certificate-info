using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CertificateViewer.Components.Dialogs.OpenUrl;
using ShadUI;
using Xunit;

using Aw = Avalonia.Controls.Window;

namespace CertificateViewer.Tests;

/// <summary>
///     Real-UI regression tests for the Open URL dialog. The <see cref="OpenUrlDialogSubmitCallbackTests" />
///     suite proves the view-model command fires the app's success callback; these tests add the one
///     layer that no test in the repo previously covered: the actual <c>OpenUrlContent</c> XAML
///     control, where the user's typing goes through the <c>TextBox.Text -> Url</c> TwoWay binding
///     and the click goes through the real Submit button's command. If the dialog's binding or the
///     button's command wiring regresses, these fail even though the VM tests stay green.
///
///     Uses Avalonia.Headless with a real window + <see cref="TestPlatform.Pump" /> so the XAML
///     visual tree and its bindings resolve deterministically.
/// </summary>
public class OpenUrlContentUiTests
{
    private static TextBox FindTextBox(Control root) =>
        root.GetVisualDescendants().OfType<TextBox>().First();

    private static Button FindButton(Control root, string name) =>
        root.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == name);

    /// <summary>
    ///     Builds the dialog exactly as the app does (<c>CreateDialog(vm).WithSuccessCallback(...).Show()</c>)
    ///     and hosts the real OpenUrlContent (sharing the same VM) in a headless window so its
    ///     visual tree and bindings resolve.
    /// </summary>
    private static (OpenUrlViewModel Vm, TextBox Box, Button Submit) BuildDialog(Action<OpenUrlViewModel>? onSuccess)
    {
        var manager = new DialogManager();
        manager.Register<OpenUrlContent, OpenUrlViewModel>();
        var vm = new OpenUrlViewModel(manager);
        manager.CreateDialog(vm)
            .WithSuccessCallback((OpenUrlViewModel m) => onSuccess?.Invoke(m))
            .WithCancelCallback(() => { })
            .Show();

        var view = new OpenUrlContent { DataContext = vm };
        new Aw { Content = view, Width = 500, Height = 400 }.Show();
        TestPlatform.Pump();

        return (vm, FindTextBox(view), FindButton(view, "Submit"));
    }

    [Fact]
    public void TypingIntoRealTextBox_FlowsIntoViewModel_ThroughBinding()
    {
        TestPlatform.RunOnUi(() =>
        {
            var (vm, box, _) = BuildDialog(_ => { });
            // The dialog's default value is shown in the bound TextBox.
            Assert.Equal("https://", box.Text);

            // The exact path a user's typing takes: the bound TextBox.Text setter writes back to the
            // VM through the TwoWay binding (UpdateSourceTrigger=PropertyChanged).
            box.Text = "https://example.com";
            TestPlatform.Pump();

            Assert.Equal("https://example.com", vm.Url);
        });
    }

    [Fact]
    public void ClickingRealSubmitButton_WithTypedValidUrl_FiresSuccessCallback()
    {
        string? submitted = null;

        TestPlatform.RunOnUi(() =>
        {
            var (_, box, submit) = BuildDialog(vm => submitted = vm.Url);
            Assert.NotNull(submit);

            box.Text = "https://example.com";
            TestPlatform.Pump();

            // The button's command is what the user's click (or HotKey Enter) runs.
            submit.Command!.Execute(null);
            TestPlatform.Pump();
        });

        Assert.Equal("https://example.com", submitted);
    }

    [Fact]
    public void ClickingRealSubmitButton_WithHostlessUrl_DoesNotFireSuccessCallback()
    {
        string? submitted = null;

        TestPlatform.RunOnUi(() =>
        {
            var (_, box, submit) = BuildDialog(vm => submitted = vm.Url);
            box.Text = "https://";
            TestPlatform.Pump();

            if (submit.IsEnabled)
            {
                submit.Command!.Execute(null);
            }
            TestPlatform.Pump();
        });

        Assert.Null(submitted);
    }
}
