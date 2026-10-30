using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace CertificateViewer.Tests;

/// <summary>
///     ReactiveUI 23+ requires explicit builder initialization (auto-initialization was removed
///     in ReactiveUI 22+). The app gets this from UseReactiveUI() in Program.cs; the test
///     assembly gets it here — a module initializer runs exactly once when the assembly is
///     first loaded, i.e. before the first test constructs a ReactiveObject-derived VM.
/// </summary>
internal static class ReactiveUiModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }
}
