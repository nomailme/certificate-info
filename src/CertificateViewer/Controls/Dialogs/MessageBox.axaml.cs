using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CertificateViewer.Controls.Dialogs;

public partial class MessageBox : Window
{
    public enum MessageBoxButtons
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }

    public enum MessageBoxResult
    {
        Ok,
        Cancel,
        Yes,
        No
    }

    public MessageBox() => AvaloniaXamlLoader.Load(this);

    public static Task<MessageBoxResult> Show(Window? parent, string text, string title, MessageBoxButtons buttons)
    {
        var messageBox = new MessageBox { Title = title };
        messageBox.FindControl<TextBlock>("Text").Text = text;
        var buttonPanel = messageBox.FindControl<StackPanel>("Buttons");

        var res = MessageBoxResult.Ok;

        void AddButton(string caption, MessageBoxResult r, bool def = false)
        {
            var btn = new Button { Content = caption };
            btn.Click += (_, __) =>
            {
                res = r;
                messageBox.Close();
            };
            buttonPanel.Children.Add(btn);
            if (def)
            {
                res = r;
            }
        }

        switch (buttons)
        {
            case MessageBoxButtons.Ok or MessageBoxButtons.OkCancel:
                AddButton("Ok", MessageBoxResult.Ok, true);
                break;
            case MessageBoxButtons.YesNo:
            case MessageBoxButtons.YesNoCancel:
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No, true);
                break;
        }

        if (buttons is MessageBoxButtons.OkCancel or MessageBoxButtons.YesNoCancel)
        {
            AddButton("Cancel", MessageBoxResult.Cancel, true);
        }


        var tcs = new TaskCompletionSource<MessageBoxResult>();
        messageBox.Closed += delegate { tcs.TrySetResult(res); };
        if (parent != null)
        {
            messageBox.ShowDialog(parent);
        }
        else
        {
            messageBox.Show();
        }
        return tcs.Task;
    }
}
