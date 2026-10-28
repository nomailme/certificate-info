# Phase 1 — Quick wins: task breakdown

> **Source:** Phase 1 of `proposed_plan.md` (§4, items 1–4) plus the items needed to meet its exit criteria (B5 HttpClient disposal, final verification).
> **Scope:** low-risk bug fixes, dead-code removal, dependency cleanup, one service re-work (R4), and the hermetic network test (R5). Nothing from Phase 2/3.
> **Size rule:** every task is ≤ one focused work session (~1–4 h). Each task should be committable on its own with a clean build + test run.

Work top-to-bottom. Dependencies are noted per task; where none are listed, the task only needs the build baseline from Task 1.

---

## 1. Baseline

- [ ] **1.1 — Establish a verified build and test baseline**
  Run `dotnet build` on the solution and `dotnet test` on the test project (offline; the google.com integration test may fail — note it). Record which tests pass/fail and confirm the only build noise is `NU1903` and environment artifacts (§2.4). This snapshot is what every later task diffs against.

## 2. Correctness bugs (B1–B9)

Each fix is a small, self-contained change; verify by building and running the affected flow manually (or by the unit tests where one exists).

- [ ] **2.1 — Fix `ViewModeProperty` registration name (B1)**
  In `CertificateDetailsPanel.axaml.cs`, register the `DirectProperty` with `nameof(ViewMode)` instead of `nameof(ShowRaw)` so the property is registered under its real name. Verify the "raw/cert" view-mode toggle in the details panel still works.

- [ ] **2.2 — Fix the "Base64" converter (B2)**
  Make `CertificateBase64Converter` match its name and the Base64 view mode: `Convert` to `Convert.ToBase64String(cert.RawData)` (or, if PEM is the intended output, rename to `CertificatePemConverter` and update the XAML resource key). Verify the copied text is valid base64/PEM respectively.

- [ ] **2.3 — Guard the empty-certificate `First()` (B3)**
  In `MainWindowVm.cs:190`, replace `Certificates.First()` with an explicit count check: assign `Certificates[0]` when non-empty, otherwise surface a "no certificates found" state instead of throwing. Verify with an import that succeeds but yields zero certificates.

- [ ] **2.4 — Null-check `dialogResult.Error` (B4)**
  In `MainWindowVm.cs:195`, remove the `!` on `dialogResult.Error!` and fall back to a generic message when `Error` is null but the result is a failure. Verify the failure path shows a message instead of an NRE.

- [ ] **2.5 — Filter null local paths in dialog file list (B6)**
  In `DialogHelper.cs:42`, drop the `x!` suppression and add `.Where(x => x is not null)` before `.ToList()` so files without a local path never reach `File.ReadAllBytesAsync`. Verify multi-file open with a file that has no local path no longer throws.

- [ ] **2.6 — Fix DER magic-byte detection (B7)**
  Replace the `{0x30, 0x82}` check in `CertificateHelper` with a proper DER check (leading SEQUENCE `0x30` + valid ASN.1 length, or attempt `X509Certificate2.Import` and treat success as DER). Add unit tests with a certificate under 256 bytes (currently misclassified) plus the existing 881-byte fixture.

- [ ] **2.7 — Fix `app.manifest` assembly identity (B8)**
  Rename `assemblyIdentity name="AvaloniaTest.Desktop"` to `CertificateViewer` in `src/CertificateViewer/app.manifest`. Confirm the app still builds and launches.

- [ ] **2.8 — Make `ChainValidator.ValidateCore` synchronous (B9)**
  Remove the fake `Task<bool>`/`out List<string>` signature in `ChainValidator.cs`; make it synchronous (return a result record or `bool` + `out errors`), and update all call sites (e.g. `TrustStorePanel`) accordingly. Update affected unit tests and verify the trust-store validation flow.

- [ ] **2.9 — Dispose `HttpClient` in `RemoteServerImporter` + add timeout (B5)**
  In `RemoteServerImporter.cs`, wrap `SocketsHttpHandler`/`HttpClient` in `using` and add a `CancellationToken`/timeout to `GetAsync`. Keep the permissive `RemoteCertificateValidationCallback` — it is a deliberate diagnostic choice (the tool must inspect invalid/self-signed certificates; chain validation remains the security gate) — and document that intent in a code comment. Do **not** change the callback behavior.

## 3. Resource, dependency, and dead-code cleanup

Most of these are independent one-line-ish changes; do them in any order after Task 1. Group note: 3.4–3.8 (csproj/manifest-level changes) can be done as a single batch if preferred.

- [ ] **3.1 — Dispose `X509Chain` and certificates in `ChainValidator` (R3)**
  Wrap `X509Chain.Create()` and the `X509Certificate2` instances in `using` so validation no longer leaks disposables. Verify with a memory/diagnostics check or at least a code review of every allocation path. *(Depends on 2.8 — do it after the signature change.)*

- [ ] **3.2 — Move MS.DI to a stable version (R6)**
  Bump `Microsoft.Extensions.DependencyInjection` from `10.0.0-rc.1` to the stable `10.0.0` (or the latest stable 10.x) in `CertificateViewer.csproj`. Build and confirm no API breakage.

- [ ] **3.3 — Resolve the vulnerable transitive dependency (R7)**
  Update the Avalonia package line (or add a direct higher patch reference to `Tmds.DBus.Protocol`) to pull a patched version, clearing `NU1903` / GHSA-xrw6-gwf8-vvr9. Verify with `dotnet list package --vulnerable --include-transitive`.

- [ ] **3.4 — Remove the no-op `UseSizeOptimizedLinq` property (Z2)**
  Delete `<UseSizeOptimizedLinq>true</UseSizeOptimizedLinq>` from `CertificateViewer.csproj` and fix the malformed whitespace on neighboring tags (`UseNativeHttpHandler`, etc.). Confirm the build output is unchanged in behavior.

- [ ] **3.5 — Drop the unused `Microsoft.AspNetCore.TestHost` reference (Z3)**
  Remove the package reference from the test project (it is referenced nowhere in test code). Confirm `dotnet test` still compiles and runs.

- [ ] **3.6 — Delete the unused `EqualityToBoolConverter` (Z5)**
  Delete `Converters/EqualityToBoolConverter.cs` (no XAML or code references). Build to confirm nothing dangles.

- [ ] **3.7 — Align xunit to one major line (D11)**
  Fix the v2-core / v3-runner mismatch in the test project: align to `xunit 2.9.x` + `xunit.runner.visualstudio 2.x` (or migrate to xunit.v3 consistently — pick v2 for minimal churn). Validate with a real `dotnet test` run, not just version bumps.

- [ ] **3.8 — Align `global.json` to SDK 10 (D12)**
  Bump `src/CertificateViewer/global.json` from SDK 9.0 to `10.0` (rollForward as today) to match the net10.0 targets. Verify `dotnet build` still resolves the right SDK. *(CI's `dotnet-version` is out of scope; note it in the commit message.)*

- [ ] **3.9 — Drop the custom `ForEach` extension (D2)**
  Replace the `EnumerableExtensions.ForEach` call sites in `ChainValidator`/`RemoteServerImporter` with ordinary `foreach` and delete the extension file (and its tests, if any). Build + test.

- [ ] **3.10 — Remove the dead `ShowRaw` property (D4)**
  Delete the `ShowRaw` `DirectProperty` and related members from `CertificateDetailsPanel` (never bound or used). *(Do after 2.1, which touches the same registration code.)*

- [ ] **3.11 — Remove the commented-out block in `AddRootCertificate` (D3)**
  Delete the commented-out `try/catch` in `TrustStorePanel.axaml.cs`. Do **not** add new error handling here — routing through `IErrorHandler` is Phase 2 (R1).

- [ ] **3.12 — Make `ThemeModeConverters` fallback explicit (D5)**
  Replace the order-dependent `Icons[0]` fallback with an explicit `TryGetValue`/named default and remove the commented-out dead dictionary entries. Verify all theme modes still render an icon.

- [ ] **3.13 — Dedupe the magic-byte helper (D1)**
  Keep a single `ValidateUsingMagicBytes` implementation; delete the dead `MagicByteHelper.ValidateUsingMagicBytes` (never called) and route callers to the one that stays. *(Do after 2.6, which rewrites the DER check in the same files.)*

## 4. `OpenCertificateService` re-work (R4)

Split into three independent commits so each stays within one session.

- [ ] **4.1 — Read each file once in `OpenCertificateService`**
  Remove the double read (`CheckAsync` then `LoadCertificate` re-reading the same file) so file contents are read exactly once and passed through. Preserve the existing public surface.

- [ ] **4.2 — Replace the TCS dialog wrapper with a proper async flow**
  Remove the `TaskCompletionSource` + `await tcs.Task` pattern that exists only to "await" a dialog callback; restructure around the dialog's actual async API (or a direct awaitable). *(Depends on 4.1.)*

- [ ] **4.3 — Friendly error mapping for unknown/unexpected types**
  Wrap the `LoadCertificate` call in try/catch and map `Unknown`/unexpected inputs to a friendly `DialogResult.CreateFail` instead of letting raw `ArgumentOutOfRangeException` reach the global handler. Verify by opening a file of an unrecognized type. *(Depends on 4.2.)*

## 5. Hermetic network test (R5)

- [ ] **5.1 — Replace the google.com test with a local TLS endpoint**
  Rewrite `RemoteServerImporterTests` to hit a local TLS endpoint (`HttpListener` with a test certificate, or an equivalent in-process server) instead of `https://google.com`. Cover the success and error paths the old test covered. *(Depends on 2.9 — uses the importer's new timeout/cancellation behavior.)*

- [ ] **5.2 — Keep one opt-in network test and verify offline test runs**
  Keep a single real-network test marked `Trait("Category", "Network")` and ensure it is excluded by default; run `dotnet test` with no network and confirm it passes cleanly. *(Depends on 5.1 and 3.7 — the runner alignment must be done for `dotnet test` to be trustworthy.)*

## 6. Phase 1 exit-criteria verification

- [ ] **6.1 — Final phase verification pass**
  Verify the phase exit criteria: no unguarded `First()`/NRE paths; `HttpClient` and `X509Chain` disposed; no `async void` left except the ones explicitly deferred to Phase 2 (R1 `GlobalErrorHandler`, R2 `TrustStorePanel` — noted as such); build is warning-clean for `NU1903`; `dotnet test` passes offline. Compare against the baseline from Task 1.1 and record the results in the task handoff.

---

## Coverage check (Phase 1 items in `proposed_plan.md` → tasks)

| Plan item | Task |
|---|---|
| B1 | 2.1 |
| B2 | 2.2 |
| B3 | 2.3 |
| B4 | 2.4 |
| B5 (dispose + timeout + document intent) | 2.9 |
| B6 | 2.5 |
| B7 | 2.6 |
| B8 | 2.7 |
| B9 | 2.8 |
| R3 | 3.1 |
| R4 (a/b/c) | 4.1 / 4.2 / 4.3 |
| R5 | 5.1, 5.2 |
| R6 | 3.2 |
| R7 | 3.3 |
| Z2 | 3.4 |
| Z3 | 3.5 |
| Z5 | 3.6 |
| D1 | 3.13 |
| D2 | 3.9 |
| D3 | 3.11 |
| D4 | 3.10 |
| D5 | 3.12 |
| D11 | 3.7 |
| D12 | 3.8 |
| Exit criteria | 1.1 (baseline), 6.1 (final verification) |

Items intentionally **not** in Phase 1 (per `proposed_plan.md`): A1–A7, R1, R2, Z1, Z4, D6, D7, D8, D9, D10 — all Phase 2/3.
