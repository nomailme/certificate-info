using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
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
            dialogManager.Register<OpenUrlContent, OpenUrlVm>();
            dialogManager.Register<PasswordDialogContent, PasswordDialogViewModel>();
            return dialogManager;
        });

        return services;
    }
}
