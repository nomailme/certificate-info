# Certificate Viewer — Refactoring Plan

> **Scope:** Plan only. No code is modified by this document.
> **Goals:** make the application **more stable**, **smaller**, and **more maintainable**, in line with **Avalonia** and **Microsoft .NET** architecture/code guidelines.
> **Verified baseline:** the full solution (`CertificateViewer.sln`) compiles with **zero C# compiler errors** on the .NET 10 SDK (only build noise is `NU1903` on a transitive dependency and environment/file-permission artifacts, see §2.4). All findings below were confirmed by reading the source, not guessed.

---

## 1. Executive summary

Certificate Viewer is a small Avalonia (11.x) desktop app, ~2.8k lines of C#/XAML across 3 projects (UI, Logic, Tests). It is already a clean starting point: a separate `CertificateViewer.Logic` class library, compiled bindings, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`, and Native AOT publish settings.

However the review surfaced a concrete set of defects and structural problems, roughly in these buckets:

- **Real bugs** (will misbehave or crash at runtime): a `DirectProperty` registered under the wrong name, a converter named "Base64" that actually exports PEM, an unguarded `First()` on a possibly-empty certificate list, a TLS validation callback that unconditionally returns `true`, a `HttpClient` that is never disposed, and a DER magic-byte check that rejects valid small certificates.
- **Stability hazards:** `async void` in two places, a static mutable singleton error handler, an `out` parameter combined with a fake-`async` `Task` method, undisposed `X509Chain`/`X509Certificate2`, a network-dependent integration test, a pre-release `Microsoft.Extensions.DependencyInjection` RC package, and a high-severity vulnerable transitive dependency.
- **MVVM / architecture violations:** business logic (commands, collections, chain validation) living in `UserControl` code-behind, views setting `DataContext = this`, and the main ViewModel exposing infrastructure services (`DialogManager`, `OpenCertificateService`) as bindable properties.
- **Size waste:** a trim-root file that `preserve="All"`s the entire app assembly (defeating trimming), a no-op MSBuild property, a large unused `Microsoft.AspNetCore.TestHost` test dependency, and two pre-release UI-kit packages.
- **Maintainability debt:** duplicated helper logic, dead code (an unused converter, dead helper methods, commented-out blocks), inconsistent naming, a 457-line `.editorconfig` that the code broadly does not follow, and an `app.manifest` still named `AvaloniaTest.Desktop`.

The plan is organized into three phases ordered by risk/effort: **Phase 1** quick wins (mostly one-line bug fixes and dead-code removal, low risk, immediate stability), **Phase 2** structural refactors (MVVM extraction, DI, resource management, trimming), and **Phase 3** long-term architecture (project boundaries, test strategy, naming/style enforcement). Each item names the file/class/pattern, the change, the rationale, the goal it serves, and a rough effort.

---

## 2. Current state

### 2.1 Project layout

```
CertificateViewer.sln
├─ src/
│  ├─ CertificateViewer/            # Avalonia UI (WinExe, net10.0, Native AOT)
│  │  ├─ Program.cs, App.axaml(.cs), GlobalStyles.axaml
│  │  ├─ ServiceCollectionExtensions.cs, Roots.xml, app.manifest, global.json
│  │  ├─ Components/
│  │  │  ├─ MainWindow/             # MainWindow.axaml(.cs), MainWindowVm.cs
│  │  │  ├─ CertificateChain/       # CertificateChainPanel.axaml(.cs)
│  │  │  ├─ CertificateDetails/     # CertificateDetailsPanel.axaml(.cs)
│  │  │  ├─ TrustStore/             # TrustStorePanel.axaml(.cs)
│  │  │  ├─ DialogManager/          # DialogManager.cs, DialogHelper.cs
│  │  │  └─ Dialogs/                # OpenUrl/, PasswordBox/ (view + vm)
│  │  ├─ Controls/Dialogs/DialogResult.cs
│  │  ├─ Converters/  (5 files)
│  │  ├─ Extensions/ImportResultExtensions.cs
│  │  ├─ Services/    (GlobalErrorHandler, OpenCertificateService, ViewModelFactory)
│  │  ├─ Validators/DomainNameValidationAttribute.cs
│  │  └─ ViewModels/BaseViewModel.cs
│  └─ CertificateViewer.Logic/      # Class lib (net10.0), no UI refs
│     ├─ CertificateType.cs, ViewMode.cs, ChainValidator.cs, EnumerableExtensions.cs
│     └─ ImportServices/            # interfaces + Der/Pem/Pfx/Remote importers,
│                                   #   CertificateHelper, MagicByteHelper, ImportResult
└─ tests/
   └─ CertificateViewer.Tests/      # xUnit (net10.0), references Logic only
      └─ ImportServices/            # 5 test files + DataStore/ fixtures
```

**Key architectural facts**
- `CertificateViewer.Logic` is a pure class library with **no** Avalonia/`System.Windows` dependency — a correct boundary to preserve.
- The UI project does **not** put its importers/validator behind DI: `OpenCertificateService` and `MainWindowVm` `new` up `DerImporter`, `PemImporter`, `PfxImporter`, `RemoteServerImporter`, and `ChainValidator` inline.
- Only `CertificateViewer.Logic` is unit-tested; the UI project has **no** tests.

### 2.2 Key dependencies

| Package | Version | Note |
|---|---|---|
| Avalonia.Desktop | 11.3.4 | stable |
| Avalonia.ReactiveUI | 11.3.4 | stable |
| CommunityToolkit.Mvvm | 8.4.0 | stable |
| Lucide.Avalonia | 0.1.43 | **pre-release (0.x)** UI kit |
| ShadUI | 0.1.5 | **pre-release (0.x)** UI kit |
| Microsoft.Extensions.DependencyInjection | **10.0.0-rc.1** | **pre-release RC in a shipped app** |
| Tmds.DBus.Protocol (transitive) | 0.21.2 | **known HIGH-severity vuln (NU1903 / GHSA-xrw6-gwf8-vvr9)** |
| Microsoft.NET.Test.Sdk | 17.14.1 | test |
| xunit | 2.9.3 | **v2** core |
| xunit.runner.visualstudio | **3.1.4** | **v3 runner — mismatched with v2 core** |
| Microsoft.AspNetCore.TestHost | 9.0.9 | **unused** (referenced nowhere in test code) |

### 2.3 Notable configuration observations

- `src/CertificateViewer/global.json` pins SDK **9.0** (rollForward latestFeature), while all three projects target **net10.0**. The CI workflow also installs `dotnet-version: 9.0.x`. (CI itself is out of scope per the task, but the in-repo `global.json` is a build-config asset and should be aligned to 10.x.)
- `src/CertificateViewer/CertificateViewer.csproj` sets `PublishAot=true`, `TrimMode=full`, `PublishTrimmed=true` — good — **but** `Roots.xml` `preserve="All"`s the whole `CertificateViewer` assembly, which nullifies most of the trimming (see §3.4).
- `.editorconfig` is a 457-line RehanSaeed template; the code routinely violates it (`this.` qualification, non-expression-bodied members, `var` style, etc.), so the rules are not actually being enforced.

### 2.4 Build/test status (verified)

- `dotnet build` of the whole solution produces **no C# (CSxxxx) errors**. The only failures in this sandbox are `MSB4018`/`MSB3374` caused by the environment refusing to `chmod`/timestamp `obj/` files — unrelated to source.
- Build emits `NU1903` (high-severity vulnerable transitive dep) and locale `NETSDK1188` warnings from the test platform.
- `dotnet test` was not run (network-dependent test + sandbox). The xunit v2-core / v3-runner mismatch and the network test are flagged as risks in §3 and Phase 1/3.

---

## 3. Issue inventory (by area)

Each item: **Location → Problem → Fix → Impact → Effort.**
Impact tags: **S**tability, **Z**ize, **M**aintainability. Effort: **S** ≤½ day, **M** ½–2 days, **L** >2 days.

### 3.1 Correctness bugs (highest value per line)

| # | Location | Problem | Fix | Impact | Effort |
|---|---|---|---|---|---|
| B1 | `CertificateDetailsPanel.axaml.cs:23-27` | `ViewModeProperty` is registered with `AvaloniaProperty.RegisterDirect<..., ViewMode>(nameof(ShowRaw), ...)` — wrong property name, so `ViewMode` is registered as `"ShowRaw"`. | Register with `nameof(ViewMode)`. | S, M | S |
| B2 | `Converters/CertificateBase64Converter.cs:24` | Named "Base64" but returns `cert.ExportCertificatePem()` (PEM, not base64). | Either `Convert` to `Convert.ToBase64String(cert.RawData)` (matching the name and the "Base64" view mode) or rename to `CertificatePemConverter` and update the XAML resource key. | S, M | S |
| B3 | `MainWindowVm.cs:190` | `SelectedCertificate = dialogResult.Certificates.First();` throws `InvalidOperationException` when the list is empty (a "success" import can be empty). | Guard: `if (result.Certificates.Count > 0) SelectedCertificate = result.Certificates[0];` and surface a "no certificates found" state otherwise. | S | S |
| B4 | `MainWindowVm.cs:195` | `dialogResult.Error!.Message` — the `!` will NRE if a failure is produced without an `Exception`. | Null-check `Error`; fall back to a generic message. | S | S |
| B5 | `ImportServices/Implementation/RemoteServerImporter.cs:26,45` | `new HttpClient(handler)` is never disposed; the `RemoteCertificateValidationCallback` unconditionally returns `true`. The permissive callback is an intentional diagnostic choice, not a defect: Certificate Viewer inspects certificates regardless of validity, so the importer must be able to load invalid or self-signed remote certificates; actual security validation happens later in the pipeline (chain validation in the TrustStore view via `ChainValidator`). | Dispose the `HttpClient`/`SocketsHttpHandler` (`using`); keep the permissive callback (it is required for inspecting invalid certificates) but document the intent explicitly in code, with the later-stage chain validation remaining the security gate. Add a `CancellationToken`/timeout to `GetAsync`. | S | M |
| B6 | `DialogManager/DialogHelper.cs:42` | `.Select(s => s.TryGetLocalPath()).Select(x => x!).ToList()` — `x!` suppresses null but leaves `null` entries when a picked file has no local path; downstream `File.ReadAllBytesAsync(null)` throws. | `.Where(x => x is not null)` before materializing. | S | S |
| B7 | `Logic/ImportServices/CertificateHelper.cs:38,43` | DER detection uses magic bytes `{0x30, 0x82}`. `0x82` is the 2-byte-length marker; DER certs under 256 bytes use a 1-byte length (`0x30, 0x??`) and are misclassified as not-DER. The bundled 881-byte `der.crt` fixture happens to match, hiding the bug. | Detect a SEQUENCE (`0x30`) plus a valid ASN.1 length, or attempt `X509Certificate2.Import` and treat success as DER. | S | S |
| B8 | `app.manifest:6` | `assemblyIdentity name="AvaloniaTest.Desktop"` — template leftover. | Rename to `CertificateViewer`. | M | S |
| B9 | `Logic/ChainValidator.cs:13` | `ValidateCore` is `Task<bool>` with an `out List<string> errors` and no `await` (fake async); `out` + async is a smell; `chain.Build` is synchronous. | Make it synchronous (`bool ValidateCore(..., out List<string> errors)` or return a result record) and drop the fake `Task`. | M | S |

### 3.2 Stability / resource management

| # | Location | Problem | Fix | Impact | Effort |
|---|---|---|---|---|---|
| R1 | `Services/GlobalErrorHandler.cs` | Static mutable singleton (`Instance`, `BeginInit`/`EndInit`, `async void OnNext`); `async void` lets exceptions escape unobserved; not thread-safe. | Register an `IErrorHandler` in DI, construct it once with its `DialogManager` dependency, make `OnNext` `Task`-based (not `async void`). | S, M | M |
| R2 | `TrustStorePanel.axaml.cs:89` | `.Subscribe(async void (_) => await UpdateValidity());` — `async void` in a constructor; exceptions are unobserved; the subscription is never disposed. | Move to a VM (see A1) where the subscription is stored and disposed with the view's `Deactivate`. | S, M | M (with A1) |
| R3 | `Logic/ChainValidator.cs:15` | `X509Chain.Create()` and the `X509Certificate2` objects are never disposed (`IDisposable` leak on every validation). | Wrap in `using`. | S | S |
| R4 | `Services/OpenCertificateService.cs:16-40` | File is read twice (`CheckAsync` then `LoadCertificate` re-reads); the TCS + `await tcs.Task` wrapper exists only to "await" a dialog callback; no try/catch around `LoadCertificate` (unknown type throws raw `ArgumentOutOfRangeException` to the global handler). | Read once; replace the TCS with a proper async dialog pattern; wrap in try/catch and map `Unknown`/unexpected to a friendly `DialogResult.CreateFail`. | S, M | M |
| R5 | `tests/.../RemoteServerImporterTests.cs` | Integration test hard-hits `https://google.com` — flaky in CI/offline. | Replace with a local TLS endpoint (`HttpListener` + a test cert) or a `TestServer`; keep one opt-in network test marked `Trait("Category","Network")`. | S, M | M |
| R6 | `CertificateViewer.csproj:33` | Pre-release `Microsoft.Extensions.DependencyInjection 10.0.0-rc.1` in a shipped app. | Move to the stable 10.0.0 (or a known-stable line) once available. | S | S |
| R7 | transitive `Tmds.DBus.Protocol 0.21.2` | High-severity CVE (NU1903). | Update the `Avalonia.Desktop`/Avalonia package line (or add a direct higher patch version) to pull a patched transitive; add `dotnet list package --vulnerable` to the build. | S | S |

### 3.3 MVVM / architecture (Avalonia guidelines)

| # | Location | Problem | Fix | Impact | Effort |
|---|---|---|---|---|---|
| A1 | `Components/TrustStore/TrustStorePanel.axaml.cs` | Business logic in code-behind: `TrustedCertificates`, `Errors`, `IsChainValid`, `UpdateValidity()` (calls `ChainValidator`), `AddRootCertificate`, `RemoveFromTrustedStore`, `async void` subscription. | Extract a `TrustStoreViewModel` (owns collections + commands + validation); the `UserControl` becomes a dumb view bound to the VM. Inject `ICertificateChainValidator` + an importer instead of `_openCertificateService!`. | M, S | L |
| A2 | `Components/CertificateChain/CertificateChainPanel.axaml.cs:38` | `DataContext = this` (view is its own DataContext) + `DirectProperty`s acting as VM state + verbose `$parent[UserControl].((vm:...)DataContext)` compiled bindings. | Remove `DataContext = this`; either introduce a small `CertificateChainViewModel` or keep the control data-driven with `x:DataType` set to the **certificate/VM type** and bind directly (Avalonia recommends binding to a view model, not to the control instance). | M | M |
| A3 | `Components/CertificateDetails/CertificateDetailsPanel.axaml.cs` | Same `DirectProperty`-as-state pattern (`Certificate`, `ViewMode`, `ShowRaw`); `x:DataType` is the `X509Certificate2` model, which is fine for the leaf bindings, but `ViewMode`/`ShowRaw` are view-state that belongs in a VM. | Move `ViewMode` (and the dead `ShowRaw`) into a VM; keep the control thin. Remove the unused `ShowRaw` (see D4). | M | M |
| A4 | `MainWindowVm.cs:67-77` + `MainWindow.axaml:97-100` | `MainWindowVm` exposes `DialogManager`, `OpenCertificateService` (infrastructure) as bindable properties and the XAML wires child panels to them. | Do not expose services on the VM. Construct child VMs in the composition root (`App.OnFrameworkInitializationCompleted`) with the services injected, and pass the **VMs** to child views (Avalonia MVVM: views bind to VMs, not to services). | M | M |
| A5 | `MainWindowVm.cs` | Single VM owns file-open, URL-load, theme-switch, title management, chain management, drag-drop orchestration (multiple responsibilities). | Split into focused pieces (e.g., a `CertificateSource`/loading service + a `Theme` concern). At minimum, extract the URL-load logic (`OpenUrl`/`Load` local functions) into a testable service. | M | M |
| A6 | `Components/MainWindow/MainWindow.axaml.cs:25-28,40-64` | Dead commented `RxApp.DefaultExceptionHandler` line + a manual `DragDropHandler` that duplicates error-dialog logic already in the VM. | Remove the dead line; keep drag-drop but route errors through the shared `IErrorHandler` (R1) rather than a second ad-hoc dialog path. | M | S |
| A7 | `App.axaml.cs:85-97` | `DisableAvaloniaDataAnnotationValidation()` strips Avalonia's `DataAnnotationsValidationPlugin` while the VMs perform their own reflection-based DataAnnotations validation — confusing duplication, marked `RequiresUnreferencedCode`. | Decide one validation path. If manual validation is kept (see D6), remove the plugin-removal hack and the `UnconditionalSuppressMessage`; if Avalonia's is preferred, remove the manual path. | M, S | M |

### 3.4 Size reduction

| # | Location | Problem | Fix | Impact | Effort |
|---|---|---|---|---|---|
| Z1 | `Roots.xml` | `<assembly fullname="CertificateViewer" preserve="All" />` preserves the **entire** app assembly, defeating `PublishTrimmed`/AOT. | Remove the `preserve="All"` for `CertificateViewer`; keep only genuinely-required roots (types reached via reflection/serialization). With compiled bindings + no reflection in `Logic`, most can be dropped. | Z | M |
| Z2 | `CertificateViewer.csproj:20` | `<UseSizeOptimizedLinq >true</UseSizeOptimizedLinq >` — non-standard no-op property **and** malformed tag whitespace. | Delete the property; fix the whitespace on neighboring tags (`UseNativeHttpHandler`, etc.). | Z, M | S |
| Z3 | `tests/.../csproj:12` | `Microsoft.AspNetCore.TestHost` (large ASP.NET surface) is referenced but never used. | Remove the package reference. | Z | S |
| Z4 | `CertificateViewer.csproj:32-34` | `Lucide.Avalonia 0.1.43` + `ShadUI 0.1.5` are both pre-release UI kits adding size and churn. | Audit how much of each is actually used; if ShadUI is only a handful of controls (`Card`, `DialogHost`, `Badge`, `ToggleSwitch`, `BusyArea`), consider replacing with the built-in `Avalonia.Controls` + Fluent theme to drop a pre-release dependency. At minimum pin to a known-stable line. | Z, S | M |
| Z5 | `Converters/EqualityToBoolConverter.cs` | Entire class unused (no XAML/code reference). | Delete the file. | Z, M | S |

### 3.5 Maintainability / dead code / duplication

| # | Location | Problem | Fix | Impact | Effort |
|---|---|---|---|---|---|
| D1 | `Logic/ImportServices/MagicByteHelper.cs:5` vs `CertificateHelper.cs:50` | Two copies of `ValidateUsingMagicBytes` (one `protected` instance method that is never called, one `private` in `CertificateHelper`). | Keep one helper; delete the dead `MagicByteHelper.ValidateUsingMagicBytes`. | M | S |
| D2 | `Logic/EnumerableExtensions.cs` | Custom `ForEach` — non-idiomatic (LINQ already has `foreach`/`ToList`); used only in `ChainValidator`/`RemoteServerImporter`. | Replace call sites with `foreach`/LINQ and delete the extension. | M | S |
| D3 | `TrustStorePanel.axaml.cs:166-184` | Commented-out `try/catch` inside `AddRootCertificate`. | Remove the dead block; restore real error handling (via `IErrorHandler`). | M | S |
| D4 | `CertificateDetailsPanel` | `ShowRaw` property/`DirectProperty` is defined but never bound or used. | Delete it. | Z, M | S |
| D5 | `Converters/ThemeModeConverters.cs` | `Icons[0]` fallback is order-dependent (Dictionary ordering) and the dictionary has commented-out dead entries. | Use an explicit `default`/`TryGetValue` with a named default; remove dead entries. | M | S |
| D6 | `ViewModels/BaseViewModel.cs` | Reflection-based `ValidateAllProperties` + `INotifyDataErrorInfo` + `[DynamicallyAccessedMembers]` + `UnconditionalSuppressMessage` — heavy reflection that is a trim/AOT risk (the suppress is still `<Pending>`). | For two simple dialog VMs, replace reflection validation with explicit per-property validation in each setter (no reflection, no suppress). Or, if DataAnnotations is desired, use a single well-annotated trim-safe path. | M, S | M |
| D7 | `Dialogs/OpenUrl/OpenUrlVm.cs` & `Dialogs/PasswordBox/PasswordDialogViewModel.cs` | Near-identical `Submit`/`Cancel` + validation logic duplicated. | Extract a `DialogViewModelBase` (shared `Submit`/`Cancel` + validation). | M | S |
| D8 | `ServiceCollectionExtensions.cs` | `RegisterDialogs(this IServiceProvider)` returns the provider; extension naming/placement is unusual (it's on `IServiceProvider`, registered after `BuildServiceProvider`). | Rename to `RegisterDialogViews(this IServiceCollection)` and call it before building the provider (standard MS.DI idiom). | M | S |
| D9 | naming: `OpenUrlVm`, `CertificateType.Web` | Inconsistent ViewModel naming (`OpenUrlVm` vs `PasswordDialogViewModel`); `CertificateType.Web` is an unclear name for "from remote server". | Standardize to `...ViewModel`; rename `Web` → `Remote`. | M | S |
| D10 | `.editorconfig` (457 lines) | Rules are broadly violated by the code; they give false confidence. | Either enforce via a CI `dotnet format --verify-no-changes` / `dotnet build -warnaserror` and fix the delta, or trim to the rules the code actually follows. | M | M |
| D11 | `tests/.../csproj` xunit | `xunit 2.9.3` (v2 core) with `xunit.runner.visualstudio 3.1.4` (v3 runner) — mismatched; can break `dotnet test`. | Align to one major line (v2: `xunit.runner.visualstudio 2.x` + `xunit 2.9.x`; or migrate to `xunit.v3` + v3 runner). Verify with a real `dotnet test` run. | S, M | S |
| D12 | `global.json` + CI | `global.json` pins SDK 9.0 but projects target net10.0 (and CI installs 9.0.x). | Bump `global.json` to `10.0` (CI dotnet version is out of scope but should be aligned separately). | S, M | S |

---

## 4. Phased plan (prioritized)

**Ordering rationale:** Phase 1 removes real defects and dead code with near-zero behavioral risk and immediate stability payoff — it also de-risks everything downstream (e.g., fixing the TLS callback and disposing resources before we restructure the classes around them). Phase 2 does the structural MVVM/DI work, which is higher risk and benefits from the clean, bug-free baseline Phase 1 produces. Phase 3 is the long-term architecture (project boundaries, test strategy, style enforcement) that is only sensible once the structure from Phase 2 is in place.

### Phase 1 — Quick wins (low risk, high stability value)
*Estimated total: ~3–5 engineer-days.*

1. **Fix correctness bugs** B1, B2, B3, B4, B6, B7, B8, B9. *(S/S)*
2. **Fix resource/handler defects** R3 (dispose `X509Chain`/certs), R6 (stable MS.DI), R7 (vulnerable transitive), Z2 (remove no-op property), Z3 (drop unused `TestHost`), Z5 (delete dead converter), D11 (align xunit), D12 (align `global.json`), D2 (drop custom `ForEach`), D4 (remove dead `ShowRaw`), D3 (remove commented-out block), D5 (converter fallback), D1 (dedupe magic-byte helper). *(S)*
3. **Re-work `OpenCertificateService`** (R4): single file read, proper async dialog flow, friendly error mapping. *(M)*
4. **Replace the network test** (R5) with a hermetic local TLS endpoint + opt-in network trait. *(M)*

**Exit criteria:** no unhandled `First()`/NRE paths; `HttpClient`/`X509Chain` disposed; no `async void` left except where noted for Phase 2; build is warning-clean for NU1903; `dotnet test` passes offline.

### Phase 2 — Structural refactors (MVVM, DI, trimming)
*Estimated total: ~2–3 weeks.*

1. **Extract `TrustStoreViewModel`** (A1, R2): move collections/commands/validation out of `TrustStorePanel.axaml.cs`; store and dispose the validation subscription on view `Deactivate`; inject `ICertificateChainValidator` and an importer. *(L)*
2. **De-"self-DataContext" the panels** (A2, A3): remove `DataContext = this`, set `x:DataType` to the bound VM/model type, bind directly; introduce thin VMs for `CertificateChain`/`CertificateDetails` view-state. *(M each)*
3. **Stop exposing services on `MainWindowVm`** (A4): construct child VMs in the composition root and pass VMs to child views. *(M)*
4. **Introduce an `IErrorHandler` in DI** (R1) and route all error dialogs (VM, drag-drop, global handler) through it; remove the static singleton and the `App` plugin-removal hack (A7). *(M)*
5. **DI the importers** (prep for Phase 3): register `DerImporter`/`PemImporter`/`PfxImporter`/`RemoteServerImporter`/`ChainValidator` in the `ServiceCollection` and inject them into `OpenCertificateService`/VMs instead of inline `new`. *(M)*
6. **Split `MainWindowVm`** (A5) and extract URL-load logic into a testable service. *(M)*
7. **Simplify `BaseViewModel` validation** (D6) — remove reflection + trim suppressions. *(M)*
8. **Consolidate dialog VMs** (D7) and fix `RegisterDialogs` idiom (D8). *(S)*
9. **Recover trimming** (Z1): remove `preserve="All"` from `Roots.xml`, add only needed roots; measure the published size before/after. *(M)*
10. **Audit UI-kit dependencies** (Z4): decide ShadUI/Lucide keep/replace; if ShadUI is only a few controls, evaluate replacing with Fluent-theme `Avalonia.Controls` to drop a pre-release dep and shrink size. *(M)*

**Exit criteria:** no business logic in any code-behind; no `DataContext = this`; no service exposed as a bindable property; a single DI-registered error path; published binary size reduced (measured).

### Phase 3 — Long-term architecture
*Estimated total: ongoing / ~2–4 weeks focused.*

1. **Enforce the project boundary:** keep `CertificateViewer.Logic` UI-free; move cross-cutting domain types that currently sit in the UI project (`OpenCertificateService`, `DialogResult`, `ImportResultExtensions`, `ChainValidator` callers) into `Logic` so the UI depends on a pure domain library and the domain is fully unit-testable.
2. **Introduce an `ICertificateImporter`/factory** and register all importers via DI; make importer selection strategy-based (use `ICertificateTypeValidator.IsSupported` to pick the loader) rather than a `switch` in `OpenCertificateService`.
3. **Testing strategy:** add UI-VM unit tests (Avalonia headless) for `MainWindowVm`, `TrustStoreViewModel`, and dialog VMs; add unit tests for `ChainValidator` and `CertificateHelper` (currently untested); keep the network test opt-in. Target: Logic coverage well above current, and every Phase-2 refactor covered.
4. **Naming & style enforcement:** standardize ViewModel naming and enum names (D9), and either enforce `.editorconfig` via CI (`dotnet format --verify-no-changes`, `-warnaserror`) or trim it to what the code actually follows (D10).
5. **Dependency hygiene:** a recurring check for vulnerable/pre-release packages (R6, R7, Z4) and a policy for stabilizing the UI-kit stack.

**Exit criteria:** `Logic` has no UI references and is fully unit-tested; importers are DI-selected; a CI style gate exists; no pre-release package in a shipped config.

---

## 5. Goals mapping

| Goal | Primary items |
|---|---|
| **Stability** | B1–B9, R1–R7 (esp. B5 TLS, B7 DER, R3/R4 resources, R6/R7 packages, R5 test flakiness) |
| **Size** | Z1 (trimming), Z2 (no-op property), Z3 (unused TestHost), Z4 (UI kits), Z5 (dead converter), D4 (dead property) |
| **Maintainability** | A1–A7 (MVVM/DI), D1–D12 (duplication, dead code, naming, style, xunit/global.json) |

---

## 6. Risks & verification

- **Trimming (Z1)** is the riskiest size item: dropping `preserve="All"` can break reflection at runtime. Mitigate by adding the UI-VM tests from Phase 3 first, then removing roots incrementally and running the full app + a `PublishAot` publish on Linux/Windows (matching CI) after each change.
- **MVVM extraction (A1–A4)** touches the XAML bindings; verify compiled bindings still resolve (`x:CompileBindings`) and that no `DataContext = this` behavior is silently relied on. Run the app manually for the file-open, URL-load, theme-switch, and trust-store flows after each extraction.
- **xunit alignment (D11)** and **global.json (D12)** must be validated with a real `dotnet test` and `dotnet build` on the target SDK, not just by reading versions.
- **TLS callback (B5)** is an intentional diagnostic choice — the tool must be able to load invalid certificates for inspection, and security enforcement is delegated to the later-stage chain validation. Any change must preserve that diagnostic capability and the later-stage security gate; document the intent in code.
- Out of scope per the task: implementing any of the above, changing app features/public API, and modifying CI/CD pipelines or non-code assets (the CI `.yml` observations in §2.3 are noted but not planned).

---

*Generated from a full read of the solution (all `.cs`, `.axaml`, `.csproj`, `.json`, `.xml`, and the CI workflow) and a verified clean compile baseline. No source files were modified.*
