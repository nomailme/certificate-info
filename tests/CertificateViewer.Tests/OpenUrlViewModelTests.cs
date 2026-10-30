using CertificateViewer.Components.Dialogs.OpenUrl;
using ShadUI;
using Xunit;

namespace CertificateViewer.Tests;

/// <summary>
///     Regression tests for the Open URL dialog validation. The dialog must accept full URLs
///     ("scheme://host") so that submitting one actually loads the certificate chain, and it
///     must reject host-less input instead of throwing.
/// </summary>
public class OpenUrlViewModelTests
{
    private readonly OpenUrlViewModel _vm = new(new DialogManager());

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://google.com")]
    [InlineData("https://sub.example.com:8443/path")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("https://1.2.3.4")]
    public void Submit_ValidUrl_SubmitsWithoutError(string url)
    {
        _vm.Url = url;
        _vm.SubmitCommand.Execute(null);

        Assert.False(_vm.HasErrors);
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("http://")]
    [InlineData("://nohost")]
    [InlineData("a b")]
    [InlineData("not a url")]
    public void Submit_InvalidUrl_ReportsErrorAndDoesNotSubmit(string url)
    {
        _vm.Url = url;
        _vm.SubmitCommand.Execute(null);

        Assert.True(_vm.HasErrors);
        Assert.NotEmpty(_vm.GetErrors(nameof(_vm.Url)));
    }

    [Fact]
    public void Submit_BlankUrl_ReportsError()
    {
        _vm.Url = "   ";
        _vm.SubmitCommand.Execute(null);

        Assert.True(_vm.HasErrors);
    }
}
