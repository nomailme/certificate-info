using System;
using CertificateViewer.Components.Dialogs.PasswordBox;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using OpenUrlContent = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlContent;
using OpenUrlViewModel2 = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlViewModel2;

namespace CertificateViewer;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider RegisterDialogs(this IServiceProvider service)
    {
        var dialogService = service.GetRequiredService<DialogManager>();
        dialogService.Register<OpenUrlContent,OpenUrlViewModel2>();
        dialogService.Register<PasswordDialogContent,PasswordDialogViewModel>();

        return service;
    }
}
