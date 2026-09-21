# 01: Start a required external app

**What to build:** The launcher can open a required external app (Steam, Discord) through its
URI scheme handed to the Windows shell, so a missing app can be brought up automatically. The
attempt fails in a distinct, catchable way when the app's URI scheme is not registered (e.g.
Discord not installed), mirroring the existing `fivem://` launcher contract.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A new seam lets the app layer start an `ExternalApp`; the real implementation opens
      `steam://` for Steam and `discord://` for Discord through the existing shell process
      seam and reuses the existing URI-scheme registration seam (no direct `ShellExecuteEx`).
- [x] Starting an external app whose URI scheme is not registered produces a distinct,
      catchable failure (no crash) — same contract as the `fivem://` launcher.
- [x] Red-green tests cover: Steam maps to `steam://`, Discord to `discord://`, the URI is
      handed to the shell, and the unregistered-scheme failure; all through the existing
      process/scheme fakes.

## Resolution

- `Launch/IExternalAppStarter` (`Task StartAsync(ExternalApp)`) + `Launch/ExternalAppStarter`:
  maps Steam→`steam://`, Discord→`discord://`; unmapped enum → no-op (mirrors the readiness
  checker's `_ => null` totality guard, defense against future enum values).
- Refactor (mandatory): extracted `Launch/UriShellStarter` shared by `GameProcessLauncher` and
  `ExternalAppStarter` (scheme check + `explorer.exe`). `GameProcessLauncher` behavior/tests
  unchanged.
- Tests: `ExternalAppStarterTests` (3) — Steam/Discord URI handed to shell, unregistered
  scheme → `Win32Exception`. Suite 222 green.

## Comments

- Spec: `.scratch/requirement-preparation/spec.md`.
- Existing fakes `FakeProcessStarter` and `FakeUriSchemeRegistration` cover the seams; no new
  test scaffolding needed.