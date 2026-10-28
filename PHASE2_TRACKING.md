# Phase 2 tracking (plan_phase_2.md, tasks 2.1–2.22)

Status legend: `done` / `doing` / `todo`. Evidence = commit(s) on branch + build/test result.
Original repo `/github/certificate-info` is a Windows 9p mount (no chmod → MSBuild fails);
all work runs in worktree `/opt/data/cache/scratch/certview-work` (branch `phase-2-work`),
merged back per wave. Build: `dotnet build -c Release -p:UseSharedCompilation=false` (0 errors;
3 pre-existing SYSLIB0057 warnings). Tests: `dotnet test -c Release` (22 pass).

| # | Task | Wave | Status | Evidence |
|---|------|------|--------|----------|
| 2.1 | Record build/size/flow baseline | 1 | done | PHASE2_BASELINE.md; build 0 err, tests 22/22, AOT binary 35,108,976 B; GUI flows not runnable (no X server/Xvfb) |
| 2.2 | IErrorHandler + DI registration | 2 | todo | — |
| 2.3 | Wire single error path, drop static singleton | 2 | todo | — |
| 2.4 | Route ad-hoc dialogs through IErrorHandler | 2 | todo | — |
| 2.5 | Extract TrustStoreViewModel | 3 | todo | — |
| 2.6 | Validity subscription into VM, disposed | 3 | todo | — |
| 2.7 | Inject ICertificateChainValidator | 3 | todo | — |
| 2.8 | Rebind CertificateChainPanel to VM | 3 | todo | — |
| 2.9 | CertificateDetails view-state to VM | 3 | todo | — |
| 2.10 | Register four importers in DI | 3 | todo | — |
| 2.11 | Constructor-inject importers into OpenCertificateService | 4 | todo | — |
| 2.12 | Inject RemoteServerImporter into URL-load path | 4 | todo | — |
| 2.13 | Construct child VMs in composition root | 4 | todo | — |
| 2.14 | Extract URL-load into testable service | 4 | todo | — |
| 2.15 | Reduce MainWindowVm to orchestrator | 4 | todo | — |
| 2.16 | Replace reflection validation with explicit | 4 | todo | — |
| 2.17 | Extract DialogViewModelBase | 4 | todo | — |
| 2.18 | Fix RegisterDialogs DI idiom | 2 | todo | — |
| 2.19 | UI-kit audit (keep/replace decision) | 2 | todo | — |
| 2.20 | Remove preserve="All" from Roots.xml | 5 | todo | — |
| 2.21 | Measure size, verify trimmed app | 5 | todo | — |
| 2.22 | Final Phase 2 verification pass | 5 | todo | — |

## Wave execution log
- **Wave 1** (2.1): executed directly by orchestrator, 2026-10-28 07:2x UTC.
