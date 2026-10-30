using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ShadUI;

namespace CertificateViewer.Tests;

/// <summary>
///     Initializes the headless Avalonia platform exactly once (with the ShadUI theme so dialog
///     controls resolve their templates) and runs actions on the Avalonia UI thread.
///
///     Avalonia 12 dispatcher change: the "UI thread" is whichever thread first creates a
///     Dispatcher, and the default ManagedDispatcherImpl does not run a background pump loop.
///     Under xunit, tests execute on rotating thread-pool threads, so the first test to touch
///     Avalonia would own the UI dispatcher and every later cross-thread
///     Dispatcher.UIThread.Invoke would block forever (nothing pumps the queue). To match
///     Avalonia 12's own HeadlessUnitTestSession design, a dedicated dispatcher thread is
///     started BEFORE any dispatcher access: it performs the headless SetupWithoutStarting
///     (so it becomes the UI thread) and then runs a PushFrame loop so cross-thread
///     Invokes from the test threads complete. ShadUI's CreateDialog(vm) constructs the
///     concrete dialog control, which requires the UI thread, so every test that wires a
///     dialog up through the real manager arranges on it via RunOnUi.
/// </summary>
internal static class TestPlatform
{
    private static readonly object Gate = new();
    private static readonly TaskCompletionSource UiReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (Gate)
        {
            if (_initialized) return;
            StartUiThread();
            _initialized = true;
        }

        // Don't let the first test thread become the UI thread.
        UiReady.Task.Wait(TimeSpan.FromMinutes(2));
    }

    public static void Pump(int jobs = 30)
    {
        EnsureInitialized();
        // Jobs are processed by the dedicated UI thread's frame loop; pump anyway so any
        // same-thread (CheckAccess) callers keep working, with a bounded loop.
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

    private static void StartUiThread()
    {
        var cts = new CancellationTokenSource();
        cts.Token.Register(() => DisposeUiThread());
        // Task (not Thread) so the thread is created by the scheduler before any test
        // thread can touch a dispatcher; the task body is the UI dispatcher thread.
        var uiThread = new Task(() =>
        {
            AppBuilder.Configure<TestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            UiReady.TrySetResult();
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var frame = new DispatcherFrame();
                    cts.Token.Register(() => frame.Continue = false);
                    Dispatcher.UIThread.PushFrame(frame);
                }
            }
            catch (Exception)
            {
                UiReady.TrySetException(new InvalidOperationException("UI dispatcher frame loop terminated."));
            }
            finally
            {
                DisposeUiThread();
            }
        });
        uiThread.Start(TaskScheduler.Default);
    }

    private static void DisposeUiThread()
    {
        lock (Gate)
        {
            if (!_initialized) return;
            _initialized = false;
        }
    }

    /// <summary>Minimal application carrying only the ShadUI theme (no DI, no main window).</summary>
    private sealed class TestApp : Application
    {
        public override void Initialize() => Styles.Add(new ShadUI.ShadTheme());
    }
}
