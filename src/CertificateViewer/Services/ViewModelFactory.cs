using System;
using CertificateViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CertificateViewer.Services;

public class ViewModelFactory(IServiceProvider serviceProvider)
{
    public T Build<T>() where T : BaseViewModel
    {
        var viewModel = serviceProvider.GetRequiredService<T>();
        return viewModel;
    }
}
