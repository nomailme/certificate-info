# Phase 2 baseline (task 2.1) — recorded 2026-10-28 UTC

Worktree: /opt/data/cache/scratch/certview-work (branch phase-2-work, HEAD 1549f59 = Phase 1 checkpoint)
Build env: .NET SDK 10.0.402, `source env.sh` (ICU 72.1.0.3 app-local), `-p:UseSharedCompilation=false`

## Build
`dotnet build -c Release -p:UseSharedCompilation=false` → **0 errors, 3 warnings** (all pre-existing
SYSLIB0057 obsolete `X509Certificate2(byte[])`/`Import` in Logic importers — out of scope for Phase 2).

## Tests
`dotnet test -c Release` → **Passed: 22, Failed: 0, Skipped: 0** (1 s).

## Publish (baseline for 2.21 size comparison)
`dotnet publish src/CertificateViewer/CertificateViewer.csproj -c Release -r linux-x64 --self-contained`
(matches CI build.yml linux matrix; csproj has PublishAot=true/PublishTrimmed=true)
- Native AOT binary `CertificateViewer`: **35,108,976 bytes (35.1 MB)**
- Full publish folder: 120 MB
- Binary starts; without a display it fails at `XOpenDisplay` (expected) — no AOT/trim runtime crash at startup.

## App flows (2.1 output)
This host has no X server and no Xvfb (no root for apt install), so the five flows
(file-open, URL-load, theme-switch, trust-store add/remove, PFX password dialog)
**cannot be run interactively here**. Flow verification for this phase relies on:
- `dotnet build` + `dotnet test` (22 tests) passing,
- code-level review of each changed path,
- AOT publish succeeding and the binary starting (fails only at XOpenDisplay).
Each task's handoff records what could and could not be verified at runtime.

## Note on original repo location
/github/certificate-info is a Windows 9p mount where chmod/apphost/timestamp
updates fail ("Operation not permitted"), so all Phase 2 work runs in the
worktree above on native Linux filesystem. The original repo's `main` branch
received the Phase 1 checkpoint commit (1549f59).
