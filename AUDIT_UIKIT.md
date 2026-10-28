# AUDIT_UIKIT.md — Task 2.19: ShadUI / Lucide UI-kit audit (Phase 2, §2.7 / Z4)

**Scope:** Read-only audit of ShadUI + Lucide usage across `src/CertificateViewer`.
**Decision-only — NO replacement is executed in this phase.** Execution is follow-up work, out of scope.
**Source of truth:** worktree `p2-w2-audit` (branch `p2-w2-audit`). (`/github/certificate-info` is a broken 9p mount and was not used.)

---

## 1. Package references (src/CertificateViewer/CertificateViewer.csproj)

| Package | Version | csproj line |
|---|---|---|
| `ShadUI` | **0.1.5** | 33 |
| `Lucide.Avalonia` | **0.1.43** | 31 |

Context: `TargetFramework = net10.0`. Other refs (not relevant to this decision):
`Avalonia.Desktop 11.3.4`, `Avalonia.ReactiveUI 11.3.4`, `CommunityToolkit.Mvvm 8.4.0`,
`Microsoft.Extensions.DependencyInjection 10.0.0`, `Tmds.DBus.Protocol 0.21.3`.

> Note: the plan referenced "lines 31 and 33". Actual lines: Lucide.Avalonia = 31, ShadUI = 33 (confirmed).
> Latest published ShadUI is **0.1.6**; there is **no 1.0 stable line** — ShadUI is still on the 0.1.x pre-release track.

---

## 2. File inventory (plan claimed "15 files" — actual is **17**)

- **7 of 8 `.axaml` files** reference ShadUI and/or Lucide.
- **10 `.cs` files** reference ShadUI (and one references Lucide).
- Total: **17 files** (7 axaml + 10 cs). `GlobalStyles.axaml` is the one axaml file that does *not* directly reference ShadUI/Lucide (it only defines empty class selectors).

`.axaml` files with the `xmlns:shadUi` namespace declared (5):
`App.axaml`, `MainWindow.axaml`, `PasswordBoxDialogContent.axaml`, `OpenUrlContent.axaml`, `CertificateChainPanel.axaml`.

`.axaml` files using `<LucideIcon>` without a ShadUI namespace (2):
`TrustStorePanel.axaml`, `CertificateDetailsPanel.axaml`.

---

## 3. ShadUI control inventory

Discrete ShadUI **controls/types** actually instantiated (`<shadUi:X>`):

| ShadUI control | # uses | `.axaml` file(s) |
|---|---|---|
| `shadUi:Window` (+ `.Hosts`) | 1 (window) | `MainWindow.axaml` |
| `shadUi:ShadTheme` | 1 | `App.axaml` |
| `shadUi:DialogHost` | 1 | `MainWindow.axaml` |
| `shadUi:BusyArea` | 1 | `MainWindow.axaml` |
| `shadUi:Card` | 4 | `MainWindow.axaml` |
| `shadUi:Badge` | 1 | `CertificateChainPanel.axaml` |

ShadUI **attached-property helpers** (not standalone controls):

| ShadUI helper | property | `.axaml` file(s) |
|---|---|---|
| `shadUi:ControlAssist` | `.Label` | `PasswordBoxDialogContent.axaml`, `OpenUrlContent.axaml` |
| `shadUi:ElementAssist` | `.FocusOnLoad` | `PasswordBoxDialogContent.axaml`, `OpenUrlContent.axaml` |

**Distinct ShadUI surface = 8** (`Window`, `ShadTheme`, `DialogHost`, `BusyArea`, `Card`, `Badge`, `ControlAssist`, `ElementAssist`).

### ShadUI DialogManager (code-level dependency — the load-bearing part)

Beyond the 6 controls above, the app's **modal-dialog + error-handling architecture is built on ShadUI's `DialogManager` / `DialogResult`**:

- **32 `DialogManager` usages** across 8 `.cs` files:
  `App.axaml.cs`, `ServiceCollectionExtensions.cs`, `Services/GlobalErrorHandler.cs`,
  `Services/OpenCertificateService.cs`, `Components/MainWindow/MainWindowVm.cs`,
  `Components/MainWindow/MainWindow.axaml.cs`, `Components/Dialogs/OpenUrl/OpenUrlVm.cs`,
  `Components/Dialogs/PasswordBox/PasswordDialogViewModel.cs` (+ `TrustStorePanel.axaml.cs`).
- This is **not** replaceable by a "handful of built-in controls" — there is no first-party Avalonia modal-dialog manager with DI registration + success/cancel callbacks.

### ShadUI theme tokens & style classes (also load-bearing)

`shadUi:ShadTheme` (App.axaml) provides the entire design-token set. **No** local `x:Key` definitions exist for these — they all come from ShadUI:

- **~14 `DynamicResource` tokens used:** `InfoColor`, `SuccessColor`, `SuccessColor20`, `WarningColor`, `WarningColor20`, `ErrorColor`, `ForegroundColor`, `ForegroundLeadColor`, `DialogBackgroundColor`, `SidebarBackgroundColor`, `TabItemsBackgroundColor`, `SwitchBackgroundColor`, `MdCornerRadius`, `LgCornerRadius`.
- **11 ShadUI style classes used** (`Classes="..."`): `Primary`, `Outline`, `Ghost`, `h3`, `h4`, `Caption`, `Muted`, `Small`, `Large`, `PasswordReveal`, `Clearable`.
  (`GlobalStyles.axaml` declares these same selectors but with **empty** `<Style>` bodies — the real definitions come from ShadUI.)

---

## 4. Lucide icon inventory

**13 distinct icon names** actually referenced (the plan's "Lucide icons" are all from `Lucide.Avalonia` 0.1.43).

From `<LucideIcon Kind="...">` in `.axaml` (9 named + 1 converter-driven):

| Icon (Kind) | # uses | `.axaml` file(s):line |
|---|---|---|
| `Shield` | 2 | `CertificateDetailsPanel.axaml:24`, `MainWindow.axaml:37` |
| `ShieldCheck` | 3 | `CertificateChainPanel.axaml:41`, `TrustStorePanel.axaml:37`, `TrustStorePanel.axaml:70` |
| `Plus` | 2 | `TrustStorePanel.axaml:129`, `TrustStorePanel.axaml:151` |
| `File` | 1 | `MainWindow.axaml:54` |
| `FileText` | 1 | `CertificateChainPanel.axaml:16` |
| `Globe` | 1 | `MainWindow.axaml:68` |
| `Store` | 1 | `TrustStorePanel.axaml:17` |
| `Info` | 1 | `TrustStorePanel.axaml:85` |
| `ChevronRight` | 1 | `CertificateChainPanel.axaml:52` |
| *(converter-driven)* | 1 | `MainWindow.axaml:116` → resolves to `SunMoon`/`Sun`/`Moon` |

From `{LucideIconContent ...}` in `.axaml` (1):

| Icon | # uses | `.axaml` file(s):line |
|---|---|---|
| `Trash2` | 1 | `TrustStorePanel.axaml:174` |

From `LucideIconKind.*` in `.cs` (3 — the converter's theme icons, `Converters/ThemeModeConverters.cs`):

| Icon | line |
|---|---|
| `SunMoon` | 13 |
| `Sun` | 16 |
| `Moon` | 19 |

Total: **14 `LucideIcon` element usages + 1 `LucideIconContent`**, covering **13 distinct named icons**.

---

## 5. Keep / Replace / Pin decision

### Decision: **KEEP ShadUI (pin 0.1.x line, keep 0.1.5) + KEEP Lucide.Avalonia (0.1.43).** Do NOT replace in this phase.

### Rationale

The plan's replacement trigger is *"if only a handful of controls are used."* On **discrete controls** that is true — only `Card`, `Badge`, `BusyArea` (plus the `Window`/`DialogHost` shell) are instantiated. **But** the real ShadUI surface is not a handful of controls:

1. **DialogManager system (the blocker).** The entire modal-dialog + error path (password prompt, open-URL prompt, global error dialog) is built on ShadUI's `DialogManager`/`DialogResult` — **32 call sites** across 8 files, wired through DI. There is **no built-in `Avalonia.Controls` equivalent**; replacing it means writing a custom modal-overlay manager + DI registration + success/cancel callback plumbing. This is multi-day rework, not a control swap, and it would collide with Phase 2's other in-flight work (2.2–2.4 error-path/DI chain, 2.13 DI refactor).
2. **Design-token theme.** `ShadTheme` supplies **~14 color/corner tokens + 11 style classes** used pervasively across every panel. Removing it means redefining the whole token set locally against the Fluent theme.
3. **Trimming/size trade-off (Z4).** ShadUI is already trimmed/published in the Phase 1 baseline (2.1). Chasing its removal is a large, high-risk refactor with low size ROI relative to the concrete Z1 task (2.20 `Roots.xml` `preserve="All"` removal), which is where the real AOT bloat lives.

Net: replacing would be a **disproportionate rework** of a load-bearing dialog + theming subsystem, triggered by a metric (control count) that understates the actual dependency. KEEP is the correct call for this phase.

### If/when replaced later (follow-up, out of scope) — per-control mapping

| ShadUI element | Replacement (built-in Avalonia + Fluent) |
|---|---|
| `shadUi:Window` | `Avalonia.Controls.Window` (drop `.Hosts`) |
| `shadUi:Card` | `Border` (or `ContentControl`) w/ background, `CornerRadius`, box-shadow from theme |
| `shadUi:Badge` | `Border` + `TextBlock` (pill shape) or `ContentControl` |
| `shadUi:BusyArea` | overlay `Border` + `ProgressRing`/`ProgressBar`, custom `IsBusy` binding |
| `shadUi:DialogHost` + `DialogManager` | **custom modal-dialog manager** (overlay + DI + callbacks) — the main rework |
| `shadUi:ControlAssist.Label` | custom floating-label `TextBox` template |
| `shadUi:ElementAssist.FocusOnLoad` | small custom attached property (low effort) |
| `shadUi:ShadTheme` | local `Styles` resource defining all ~14 tokens + 11 classes over the Fluent base |
| `Lucide.Avalonia` (13 icons) | keep as-is (lightweight) **or** swap to `Icon` / static resource; low priority |

### Version pin (concrete)

- **ShadUI:** pin the **0.1.x line**. Keep **0.1.5** (already builds in the Phase 1/2.1 baseline — no new variable for a trim-focused phase). **0.1.6** is the latest published and is an acceptable non-blocking bump if a bump is wanted, but it is not required for this decision and is **not executed here**. There is no 1.0 line to pin to yet.
- **Lucide.Avalonia:** keep **0.1.43** (icon-only, 13 icons in use, negligible size impact).

---

## 6. Out-of-scope confirmation

- **No replacement is executed in this phase.** This is a **decision-only** audit (task 2.19).
- No source code, csproj, or XAML file was modified.
- Any actual replacement (Section 5 table) is **follow-up work, out of this phase's subtask scope**, and would need to be scheduled as its own task with its own risk/size assessment.

---

*Method: `grep -rn` across `src/CertificateViewer` for `shadUi:` tag openers, `xmlns:shadUi`, `LucideIcon`/`LucideIconContent`/`LucideIconKind`, `shadui|lucide` (case-insensitive) per file, and `DynamicResource`/`x:Key` token resolution; each finding verified by reading the matching lines. Icon inventory cross-checked with a line-aware scan (multi-line `<LucideIcon>` tags handled).*
