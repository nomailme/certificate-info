using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using CertificateViewer.Logic;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;
using CertificateViewer.Services;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using OpenUrlContent = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlContent;

namespace CertificateViewer;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the <see cref="DialogManager" /> singleton and the dialog views it can show,
    ///     using the standard <see cref="IServiceCollection" /> idiom (called before
    ///     <see cref="ServiceProviderServiceExtensions.BuildServiceProvider(IServiceCollection)" />).
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <returns>The service collection, so calls can be chained.</returns>
    public static IServiceCollection RegisterDialogViews(this IServiceCollection services)
    {
        // The factory creates the singleton itself and registers the dialog views on it. It must
        // not resolve DialogManager from the provider: that would re-enter this factory (MS.DI
        // detects self-referential singletons and throws).
        services.AddSingleton(_ =>
        {
            var dialogManager = new DialogManager();
            dialogManager.Register<OpenUrlContent, OpenUrlViewModel>();
            dialogManager.Register<PasswordDialogContent, PasswordDialogViewModel>();
            return dialogManager;
        });

        return services;
    }

    /// <summary>
    ///     Registers the certificate pipeline services that <see cref="IOpenCertificateService" />
    ///     (concrete <see cref="OpenCertificateService" />) needs to activate: the file importers
    ///     under the strategy interface, the password prompt, and the remote URL loader. This is
    ///     the single source of truth for that wiring — <see cref="CertificateViewer.App" /> calls
    ///     it at startup and the test project calls it to verify container resolution, so a
    ///     missing or mistyped registration (e.g. an importer registered under its concrete type
    ///     only) fails the regression tests instead of the app at runtime.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The service collection, so calls can be chained.</returns>
    public static IServiceCollection RegisterCertificateServices(this IServiceCollection services)
    {
        // File importers are registered under the strategy interface; OpenCertificateService
        // receives them as an IEnumerable and picks the first that supports the input bytes.
        services.AddSingleton<ICertificateFileImporter, DerImporter>();
        services.AddSingleton<ICertificateFileImporter, PemImporter>();
        services.AddSingleton<ICertificateFileImporter, PfxImporter>();
        services.AddSingleton<IPasswordPrompt, PasswordPrompt>();
        services.AddSingleton<RemoteServerImporter>();
        services.AddSingleton<ICertificateLoader<string>>(sp => sp.GetRequiredService<RemoteServerImporter>());
        services.AddSingleton<OpenCertificateService>();
        // The view models program against the service interface so tests can substitute a fake;
        // the concrete implementation is the single registered instance.
        services.AddSingleton<IOpenCertificateService>(sp => sp.GetRequiredService<OpenCertificateService>());
        return services;
    }
}
