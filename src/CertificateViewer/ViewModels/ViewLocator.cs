using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace CertificateViewer.ViewModels;

public class ViewLocator: IDataTemplate
{
    public IControl Build(object param)
    {
        var name = param.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock
        {
            Text = "Not Found: " + name
        };
    }

    public bool Match(object data) => data is BaseViewModel;
}
