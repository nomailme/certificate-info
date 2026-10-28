# Phase 2 — Structural refactors: task breakdown

> **Source:** Phase 2 of `proposed_plan.md` (§4, items 1–10) and its exit criteria.
> **Scope:** MVVM extraction, DI wiring, a single DI-registered error path, and trimming recovery. Nothing from Phase 1 (already executed) or Phase 3 (project boundaries, test strategy, style enforcement, D9/D10).
> **State note:** written against the repo **as it stands after the Phase 1 execution** (`plan_phase_1.md` items 1.1–6.1 done). Where a task restates the "before" state of a file, that is the post-Phase-1 state.
> **Size rule:** every task is ≤ one focused work session (~1–4 h) and committable on its own with a clean build. Large plan items are split (A1+R2 → 2.5–2.7; R1+A7+A6 → 2.2–2.4).

**Format per task:** ID — imperative title (≤ 80 chars), 1–3 sentence description, **Inputs**, **Outputs**, **Deps** (empty = parallelizable with everything else in the same wave), **Effort** (h), **Wave**.

---

## 2.0 Baseline

- [ ] **2.1 — Record the Phase 2 build, size, and flow baseline**
  Confirm the solution builds and `dotnet test` passes offline; run a `PublishAot` publish and record the published binary size; run the five app flows (file-open, URL-load, theme-switch, trust-store add/remove, PFX password dialog) and record any defects in the task handoff. This snapshot is what 2.22 verifies against.
  - **Inputs:** post-Phase-1 repo; `dotnet` 10 SDK.
  - **Outputs:** recorded build/test/publish status, published size figure, and flow notes in the task handoff.
  - **Deps:** none.
  - **Effort:** 2 h. **Wave:** 1.

## 2.1 Error path (R1, A7, A6)

- [ ] **2.2 — Define IErrorHandler and register it in DI**
  Add an `IErrorHandler` interface (an async, task-based `HandleAsync(Exception, ...)` — no `async void`) and a `GlobalErrorHandler` implementation that shows its error dialog through the injected `ShadUI.DialogManager`. Register both as singletons in `App.OnFrameworkInitializationCompleted`'s `ServiceCollection`.
  - **Inputs:** `Services/GlobalErrorHandler.cs` (current static singleton); `App.axaml.cs` (composition root).
  - **Outputs:** `IErrorHandler` interface file; rewritten `GlobalErrorHandler`; DI registration in `App`.
  - **Deps:** 2.1.
  - **Effort:** 2 h. **Wave:** 2.

- [ ] **2.3 — Wire the single error path and drop the static singleton**
  Replace the `GlobalErrorHandler.Instance` usage in `App.OnUnhandledException` and the `RxApp.DefaultExceptionHandler` hookup in `BeginInit`/`EndInit` with the DI-resolved `IErrorHandler`; delete the static `Instance`/`BeginInit`/`EndInit` members and the `async void OnNext` path. Keep the unhandled-exception behavior observable (exception still surfaces a dialog).
  - **Inputs:** `App.axaml.cs` (lines 29–33, 41, 81); `Services/GlobalErrorHandler.cs`.
  - **Outputs:** `App` resolves one DI-registered error handler; no static singleton remains; no `async void` on the error path.
  - **Deps:** 2.2.
  - **Effort:** 2 h. **Wave:** 2.

- [ ] **2.4 — Route all ad-hoc error dialogs through IErrorHandler**
  Replace the inline `DialogManager.CreateDialog("…", e.Message)` error paths in `MainWindowVm` (the URL-load `Load` catch at ~line 139 and `LoadCertificates` at ~line 196) and in `MainWindow.axaml.cs` drag-drop (lines ~40–64) with calls to the injected `IErrorHandler`. Delete the dead commented `RxApp.DefaultExceptionHandler` line in `MainWindow.axaml.cs`.
  - **Inputs:** `MainWindowVm.cs`; `MainWindow.axaml.cs`; `IErrorHandler` from 2.2.
  - **Outputs:** single error-dialog path; dead line removed; `MainWindow.axaml.cs` no longer duplicates dialog logic.
  - **Deps:** 2.3.
  - **Effort:** 3 h. **Wave:** 2.

## 2.2 TrustStore MVVM extraction (A1, R2)

- [ ] **2.5 — Extract TrustStoreViewModel (collections and commands)**
  Create `TrustStoreViewModel` (CommunityToolkit.Mvvm) owning `TrustedCertificates`, `Errors`, `IsChainValid`, `UseSystemStore`, and the `AddRootCertificate`/`RemoveFromTrustedStore` commands; move the logic out of `TrustStorePanel.axaml.cs`. The panel becomes a dumb view bound to the VM, and `AddRootCertificate` routes file-loading errors through `IErrorHandler` instead of silently dropping failures.
  - **Inputs:** `TrustStorePanel.axaml.cs` (182 lines of code-behind); `TrustStorePanel.axaml`; `IErrorHandler` (2.2); `OpenCertificateService`.
  - **Outputs:** `ViewModels/TrustStoreViewModel.cs` (or equivalent); `TrustStorePanel.axaml.cs` reduced to `InitializeComponent` + `Deactivate` only.
  - **Deps:** 2.4.
  - **Effort:** 3 h. **Wave:** 3.

- [ ] **2.6 — Move the validity subscription into the VM and dispose it**
  Move the `WhenAnyValue(TrustedCertificates, CertificateChain, UseSystemStore).Subscribe(async void …)` chain from the panel constructor into the VM as a stored `CompositeDisposable`, disposed on `Deactivate`/`OnDeactivate` — eliminating the `async void` subscription and the undisposed subscription. Verify re-validation fires when the chain, trusted certs, or store switch changes.
  - **Inputs:** `TrustStoreViewModel` (2.5); `TrustStorePanel.axaml.cs` (lines 84–91).
  - **Outputs:** no `async void` in TrustStore; subscription disposed with the view lifecycle; re-validation verified in the trust-store flow.
  - **Deps:** 2.5.
  - **Effort:** 2 h. **Wave:** 3.

- [ ] **2.7 — Inject ICertificateChainValidator into TrustStoreViewModel**
  Replace `new ChainValidator()` in the validity update with constructor-injected `ICertificateChainValidator` (introduce the interface in `CertificateViewer.Logic`, implemented by `ChainValidator`) and register it as a singleton in the `ServiceCollection` (the importer registrations in 2.10 then only add the four importers). `TrustStorePanel` no longer references `CertificateViewer.Logic` or `new`s any Logic type.
  - **Inputs:** `CertificateViewer.Logic/ChainValidator.cs`; `TrustStoreViewModel` (2.5); `App.axaml.cs` ServiceCollection.
  - **Outputs:** `ICertificateChainValidator` interface + registration; TrustStore VM free of inline `new` of Logic types.
  - **Deps:** 2.6.
  - **Effort:** 1.5 h. **Wave:** 3.

## 2.3 De-self-DataContext the panels (A2, A3)

- [ ] **2.8 — Rebind CertificateChainPanel to a VM (remove DataContext = this)**
  Introduce a thin `CertificateChainViewModel` for the panel's view-state, set `x:DataType` on the control to the VM type, remove `DataContext = this`, and replace the `$parent[UserControl].((vm:…)DataContext)` relative bindings with direct VM/model bindings. The panel's `DataContext` is set externally by its host (see 2.13).
  - **Inputs:** `CertificateChainPanel.axaml`/`.axaml.cs` (58 lines of code-behind); host wiring in `MainWindow.axaml`.
  - **Outputs:** no `DataContext = this` in the panel; compiled bindings (`x:CompileBindings`) still resolve; chain items render and select identically.
  - **Deps:** 2.1.
  - **Effort:** 3 h. **Wave:** 3.

- [ ] **2.9 — Move CertificateDetails view-state into a VM**
  Move `ViewMode` (the view-state `DirectProperty`) into a thin `CertificateDetailsViewModel`; keep leaf bindings to the `X509Certificate2` model where they are already correct. Remove the remaining `DataContext = this` pattern and update `x:DataType` accordingly. Verify the raw/base64 toggle, the copy action (B2 output), and certificate rendering still work.
  - **Inputs:** `CertificateDetailsPanel.axaml`/`.axaml.cs` (43 lines, post-Phase-1 B1/B2/D4 state); host wiring in `MainWindow.axaml`.
  - **Outputs:** view-state lives in the VM; no `DataContext = this`; details panel behaves as before.
  - **Deps:** 2.1.
  - **Effort:** 3 h. **Wave:** 3.

## 2.4 DI the importers (prep for Phase 3)

- [ ] **2.10 — Register the four importers in the ServiceCollection**
  Add singleton registrations for `DerImporter`, `PemImporter`, `PfxImporter`, and `RemoteServerImporter` in `App.OnFrameworkInitializationCompleted` (the `ChainValidator`/`ICertificateChainValidator` registration is handled by 2.7). Do **not** introduce the `ICertificateImporter`/factory strategy — that stays Phase 3.
  - **Inputs:** `App.axaml.cs` (lines 49–59); `CertificateViewer.Logic/ImportServices/Implementation/*`.
  - **Outputs:** importers resolvable from DI; no inline `new` of importers remains in services/VMs (covered by 2.11/2.12 consumers).
  - **Deps:** 2.7 (same wave — the validator registration in `App` must land first).
  - **Effort:** 1.5 h. **Wave:** 3.

- [ ] **2.11 — Constructor-inject importers into OpenCertificateService**
  Convert `OpenCertificateService` to take its importers (or a small lookup) via constructor instead of `new`-ing `DerImporter`/`PemImporter`/`PfxImporter` inside `LoadCertificate`; register it in DI and inject it into `MainWindowVm` and `TrustStoreViewModel` instead of the inline `new OpenCertificateService(...)` in `MainWindowVm`'s constructor.
  - **Inputs:** `Services/OpenCertificateService.cs` (71 lines); `MainWindowVm.cs` (line 46); registrations from 2.10.
  - **Outputs:** zero inline `new` of importers anywhere in the UI project; `OpenCertificateService` resolved from DI.
  - **Deps:** 2.10.
  - **Effort:** 2 h. **Wave:** 4.

- [ ] **2.12 — Inject RemoteServerImporter into the URL-load path**
  `MainWindowVm.OpenUrl`/`Load` currently news up `RemoteServerImporter` inline (line 118); resolve it from DI (or from the URL-load service created in 2.14) so no importer is constructed at call sites.
  - **Inputs:** `MainWindowVm.cs`; registrations from 2.10.
  - **Outputs:** URL-load uses a DI-resolved importer; no `new RemoteServerImporter()` in UI code.
  - **Deps:** 2.11.
  - **Effort:** 1 h. **Wave:** 4.

## 2.5 Composition root and MainWindowVm (A4, A5, A6)

- [ ] **2.13 — Construct child VMs in the composition root**
  Stop exposing `DialogManager` and `OpenCertificateService` as bindable properties on `MainWindowVm`; construct `TrustStoreViewModel`/`CertificateChainViewModel`/`CertificateDetailsViewModel` in `App.OnFrameworkInitializationCompleted` with their dependencies injected, and pass the **VMs** to the child views (set `DataContext` on each child control from the root). Update `MainWindow.axaml` to bind to the VM properties instead of the removed service properties.
  - **Inputs:** `MainWindowVm.cs` (lines 67–77), `MainWindow.axaml` (lines ~97–100), `App.axaml.cs`; child VMs from 2.5/2.8/2.9.
  - **Outputs:** no service exposed as a bindable property; child views bound to VMs; all five flows (file-open, URL-load, theme, trust-store, PFX dialog) re-verified.
  - **Deps:** 2.5, 2.6, 2.7, 2.8, 2.9, 2.11.
  - **Effort:** 4 h. **Wave:** 4.

- [ ] **2.14 — Extract URL-load logic into a testable service**
  Move the `OpenUrl`/`Load` local functions (URI validation, `RemoteServerImporter.ImportAsync`, error mapping, `CertificateSource`/`Title` updates) into a `CertificateSourceLoader` service (constructor: importer + `IErrorHandler`) with a public `LoadAsync(string uri, ...)`-shaped surface; `MainWindowVm` delegates to it. Preserve the busy indicator and dialog-trigger behavior.
  - **Inputs:** `MainWindowVm.cs` (lines 100–146); `IErrorHandler` (2.2); importer registration (2.10).
  - **Outputs:** `Services/CertificateSourceLoader.cs` (or equivalent); URL-load logic unit-testable without the UI.
  - **Deps:** 2.4, 2.12.
  - **Effort:** 3 h. **Wave:** 4.

- [ ] **2.15 — Reduce MainWindowVm to a focused orchestrator**
  After 2.13/2.14, `MainWindowVm` should hold only chain/selection/title/busy/theme state plus thin commands (`OpenCertificateFile`, `SwitchTheme`) — remove any remaining duplicated responsibility (e.g., residual dialog logic) so the VM is a single-orchestration concern. Verify the full flow set again.
  - **Inputs:** `MainWindowVm.cs` (post-2.13/2.14).
  - **Outputs:** a `MainWindowVm` whose remaining surface is state + delegation; flows verified.
  - **Deps:** 2.13, 2.14.
  - **Effort:** 2 h. **Wave:** 4.

## 2.6 ViewModel consolidation (D6, D7, D8)

- [ ] **2.16 — Replace BaseViewModel reflection validation with explicit validation**
  Replace the reflection-based `ValidateAllProperties` + `INotifyDataErrorInfo` + `[DynamicallyAccessedMembers]` + `UnconditionalSuppressMessage` machinery in `ViewModels/BaseViewModel.cs` with explicit per-property validation in each dialog VM's setters (no reflection, no trimming suppressions). Remove the `DisableAvaloniaDataAnnotationValidation()` hack and its `[UnconditionalSuppressMessage]` from `App.axaml.cs` so a single, trim-safe validation path remains.
  - **Inputs:** `ViewModels/BaseViewModel.cs` (105 lines); `App.axaml.cs` (lines 35–37, 47, 85–97); the two dialog VMs.
  - **Outputs:** no reflection-based validation, no `<Pending>` suppressions; dialog validation behaves identically.
  - **Deps:** 2.1 (independent of 2.2–2.15 — runs in wave 4).
  - **Effort:** 3 h. **Wave:** 4.

- [ ] **2.17 — Extract a shared DialogViewModelBase**
  `OpenUrlVm` and `PasswordDialogViewModel` duplicate near-identical `Submit`/`Cancel` + validation logic; extract a `DialogViewModelBase` holding the shared commands and validation-error handling, and make both dialog VMs derive from it. Keep each VM's own properties/behavior.
  - **Inputs:** `Dialogs/OpenUrl/OpenUrlVm.cs`; `Dialogs/PasswordBox/PasswordDialogViewModel.cs`.
  - **Outputs:** `DialogViewModelBase`; both dialogs behave identically; duplication removed.
  - **Deps:** 2.16 (validation shape is defined by the base-VM rewrite).
  - **Effort:** 2 h. **Wave:** 4.

- [ ] **2.18 — Fix the RegisterDialogs DI idiom**
  Rename `RegisterDialogs(this IServiceProvider)` to `RegisterDialogViews(this IServiceCollection)`, change it to an `IServiceCollection` extension, and call it **before** `BuildServiceProvider()` in `App` (standard MS.DI idiom) instead of returning the provider after the fact.
  - **Inputs:** `ServiceCollectionExtensions.cs` (20 lines); `App.axaml.cs` (line 59).
  - **Outputs:** standard DI extension idiom; dialog views still resolve.
  - **Deps:** 2.1.
  - **Effort:** 1 h. **Wave:** 2 (independent of 2.2–2.4 — safe to run in wave 4 as well if `App.axaml.cs` is busy).

## 2.7 Size: UI-kit audit (Z4)

- [ ] **2.19 — Audit ShadUI/Lucide usage and record the keep/replace decision**
  Inventory which ShadUI controls (`Card`, `DialogHost`, `Badge`, `ToggleSwitch`, `BusyArea`, …) and which Lucide icons are actually referenced across the XAML; decide, per the plan, whether ShadUI can be replaced by built-in `Avalonia.Controls` + Fluent theme (if only a handful of controls are used) and which version line to pin. Record the decision, with the control list, in the task handoff — **do not execute the replacement** (that is follow-up work, out of this phase's subtask scope).
  - **Inputs:** all `.axaml` under `src/CertificateViewer` (15 files reference ShadUI/Lucide); `CertificateViewer.csproj` (lines 31, 33).
  - **Outputs:** audit table (control → ShadUI/Lucide/built-in) + keep/replace/pin decision in the handoff.
  - **Deps:** 2.1.
  - **Effort:** 3 h. **Wave:** 2.

## 2.8 Trimming recovery (Z1)

- [ ] **2.20 — Remove the assembly-wide preserve from Roots.xml**
  Delete `<assembly fullname="CertificateViewer" preserve="All" />` from `Roots.xml`; add back only the specific type/assembly roots that AOT/trimming actually requires (iterating on trim warnings and runtime failures). Keep the `Avalonia.Themes.Fluent` root as-is.
  - **Inputs:** `Roots.xml` (5 lines); trim warnings from `PublishAot`; the flows from the 2.1 baseline.
  - **Outputs:** trimmed `Roots.xml` with minimal roots; `dotnet publish` succeeds.
  - **Deps:** 2.13 (all MVVM extraction done, so the final binding/reflection surface is known before roots are finalized).
  - **Effort:** 4 h. **Wave:** 5.

- [ ] **2.21 — Measure published size and verify the trimmed app end-to-end**
  Run a full `PublishAot` publish (Linux, matching CI), record the final binary size against the 2.1 baseline (exit criterion: size reduced), and smoke-test the published binary through all five flows plus the trust-store validation path — the runtime reflection check that `preserve="All"` used to paper over.
  - **Inputs:** `Roots.xml` (2.20); 2.1 baseline size; published build output.
  - **Outputs:** before/after size figures; pass/fail per flow recorded in the handoff.
  - **Deps:** 2.20.
  - **Effort:** 2 h. **Wave:** 5.

## 2.9 Phase 2 exit-criteria verification

- [ ] **2.22 — Final Phase 2 verification pass**
  Verify every exit criterion: no business logic in any code-behind; no `DataContext = this` anywhere; no service exposed as a bindable property; a single DI-registered error path; no `async void` in the UI project; no inline `new` of importers/validators; published size reduced vs. the 2.1 baseline. Run `dotnet build` + `dotnet test` offline and the five app flows, and record results in the handoff.
  - **Inputs:** full repo state; 2.1 baseline; 2.21 size/flow results.
  - **Outputs:** exit-criteria checklist (pass/fail per criterion) in the handoff.
  - **Deps:** 2.21 (i.e., all of 2.2–2.21).
  - **Effort:** 2 h. **Wave:** 5.

---

## Parallel execution waves

Tasks in the same wave share no mutual dependencies and can be assigned to parallel workers. Waves are the recommended order, not a strict gate — any task may start as soon as its **Deps** list is satisfied.

| Wave | Tasks (parallel within wave) | Notes |
|---|---|---|
| 1 | 2.1 | Everything else waits for the baseline. |
| 2 | 2.2→2.3→2.4 (chain), 2.18, 2.19 | 2.2→2.3→2.4 is serial; 2.18 and 2.19 run parallel to that chain. |
| 3 | 2.5→2.6→2.7 (chain), 2.8, 2.9, 2.10 | 2.5→2.6→2.7 serial; 2.8 and 2.9 parallel; 2.10 after 2.7. |
| 4 | 2.11→2.12 (chain), 2.13, 2.14, 2.15, 2.16→2.17 (chain) | 2.13 needs the whole wave-3 set; 2.14 needs 2.12; 2.15 needs 2.13+2.14; 2.16→2.17 serial, independent of the rest. |
| 5 | 2.20→2.21→2.22 | 2.20→2.21→2.22 serial; 2.22 closes the phase. |

Critical path: 2.1 → 2.2 → 2.3 → 2.4 → 2.5 → 2.6 → 2.7 → 2.10 → 2.11 → 2.13 → 2.15 → 2.20 → 2.21 → 2.22.

## Coverage check (Phase 2 items in `proposed_plan.md` → tasks)

| Plan item | Task(s) |
|---|---|
| 1. Extract TrustStoreViewModel (A1, R2) | 2.5, 2.6, 2.7 |
| 2. De-self-DataContext the panels (A2, A3) | 2.8, 2.9 |
| 3. Stop exposing services on MainWindowVm (A4) | 2.13 |
| 4. IErrorHandler in DI, single error path (R1, A7; A6 drag-drop) | 2.2, 2.3, 2.4 |
| 5. DI the importers (prep for Phase 3) | 2.10, 2.11, 2.12 |
| 6. Split MainWindowVm / URL-load service (A5) | 2.14, 2.15 |
| 7. Simplify BaseViewModel validation (D6) | 2.16 |
| 8. Consolidate dialog VMs (D7) + RegisterDialogs idiom (D8) | 2.17, 2.18 |
| 9. Recover trimming (Z1) | 2.20, 2.21 |
| 10. Audit UI-kit dependencies (Z4) | 2.19 |
| Exit criteria | 2.1 (baseline), 2.22 (final verification) |

Items intentionally **not** in this file (Phase 3 per `proposed_plan.md`): `ICertificateImporter`/factory strategy, Logic project-boundary moves, UI-VM test strategy, D9 (naming: `OpenUrlVm`, `CertificateType.Web`), D10 (`.editorconfig` enforcement).
