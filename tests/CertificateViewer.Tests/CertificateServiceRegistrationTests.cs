using CertificateViewer;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace CertificateViewer.Tests;

/// <summary>
///     Regression tests for the container wiring that the app uses at startup. The original bug:
///     importers were registered only under their concrete types, so
///     <c>IReadOnlyList&lt;ICertificateFileImporter&gt;</c> (MS.DI only auto-resolves
///     <c>IEnumerable&lt;T&gt;</c> from multiple <c>T</c> registrations) and the app crashed at
///     startup with InvalidOperationException. These tests build the same
///     service registrations <see cref="App" /> uses and resolve the services it resolves at
///     startup, so any missing or mistyped registration fails here instead of at runtime.
/// </summary>
public class CertificateServiceRegistrationTests
{
    /// <summary>
    ///     Mirrors the certificate-pipeline registrations in <see cref="App" />'s composition
    ///     root (dialog views, dialog view models, and the certificate services).
    /// </summary>
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.RegisterDialogViews();
        services.AddTransient<OpenUrlViewModel>();
        services.AddTransient<PasswordDialogViewModel>();
        services.RegisterCertificateServices();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void OpenCertificateService_Resolves_FromContainer()
    {
        using var provider = BuildServiceProvider();

        var service = provider.GetRequiredService<OpenCertificateService>();

        Assert.NotNull(service);
        // The interface registration hands out the same singleton instance the concrete
        // registration creates (the view models program against the interface).
        var viaInterface = provider.GetRequiredService<IOpenCertificateService>();
        Assert.Same(service, viaInterface);
    }

    [Fact]
    public void AllFileImporters_AreRegistered_UnderStrategyInterface()
    {
        using var provider = BuildServiceProvider();

        var importers = provider.GetServices<ICertificateFileImporter>().ToList();

        Assert.Equal(3, importers.Count);
        Assert.Contains(importers, i => i is DerImporter);
        Assert.Contains(importers, i => i is PemImporter);
        Assert.Contains(importers, i => i is PfxImporter);
    }

    [Fact]
    public async Task ResolvedOpenCertificateService_LoadsPemFile_EndToEnd()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<OpenCertificateService>();
        var path = Path.Combine(Path.GetTempPath(), $"certtest_{Guid.NewGuid():N}.pem");
        File.WriteAllBytes(path, TestCertificates.ToPem(TestCertificates.SelfSigned()));

        try
        {
            var result = await service.OpenFile(path);

            Assert.Equal(DialogResult.OperationResult.Success, result.Success);
            Assert.Single(result.Certificates);
            Assert.Equal(CertificateType.Pem, result.Type);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
