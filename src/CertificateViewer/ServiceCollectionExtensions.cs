using System;
using CertificateViewer.Components.Dialogs.OpenUrl;
using CertificateViewer.Components.Dialogs.PasswordBox;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using OpenUrlContent = CertificateViewer.Components.Dialogs.OpenUrl.OpenUrlContent;

namespace CertificateViewer;

public static class ServiceCollectionExtensions
{
    public static IServiceProvider RegisterDialogs(this IServiceProvider service)
    {
        var dialogService = service.GetRequiredService<DialogManager>();
        dialogService.Register<OpenUrlContent,OpenUrlVm>();
        dialogService.Register<PasswordDialogContent,PasswordDialogViewModel>();

        return service;
    }
}
