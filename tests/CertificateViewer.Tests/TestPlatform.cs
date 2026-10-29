using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ShadUI;

namespace CertificateViewer.Tests;

/// <summary>
///     Initializes the headless Avalonia platform exactly once (with the ShadUI theme so dialog
///     controls resolve their templates) and runs actions on the Avalonia UI thread. ShadUI's
///     <c>CreateDialog(vm)</c> constructs the concrete dialog control, which requires the UI
///     thread, so every test that wires a dialog up through the real manager must arrange on it.
/// </summary>
internal static class TestPlatform
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (typeof(TestPlatform))
        {
            if (_initialized) return;
            if (Application.Current is null)
            {
                AppBuilder.Configure<TestApp>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
            }

            _initialized = true;
        }
    }

    public static void Pump(int jobs = 30)
    {
        for (var i = 0; i < jobs; i++)
        {
            Dispatcher.UIThread.RunJobs();
        }
    }

    public static T RunOnUi<T>(Func<T> action)
    {
        EnsureInitialized();
        return Dispatcher.UIThread.CheckAccess() ? action() : (T)Dispatcher.UIThread.Invoke(action);
    }

    public static void RunOnUi(Action action)
    {
        EnsureInitialized();
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    /// <summary>Minimal application carrying only the ShadUI theme (no DI, no main window).</summary>
    private sealed class TestApp : Application
    {
        public override void Initialize() => Styles.Add(new ShadUI.ShadTheme());
    }
}
