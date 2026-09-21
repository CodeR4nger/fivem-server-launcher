Status: ready-for-agent
Type: spec

# Requirement preparation: start missing Steam/Discord and check them before connecting

## Problem Statement

When a server's effective requirements include Steam or Discord and the app is not running, the launcher currently aborts the connect with "Requires Steam (not running)" / "Requires Discord (not running)". The player must start the missing app manually, then retype the address and connect again. DOC.md's UX wants preparation instead: when something needs intervention, the launcher shows "Preparing the game... / Starting Steam..." and only connects once the app is running; when everything is ready it shows nothing extra ("don't bother if everything is ready").

## Solution

On connect, after the effective requirements are resolved, the launcher finds the required apps that are not running and automatically *starts* each of them, showing its progress in the status line ("Starting Steam..."), then *checks* (polls readiness) until the app is running, bounded by a timeout, and only then proceeds to launch. If an app cannot be started (its URI scheme is unregistered, e.g. Discord not installed) or never becomes running within the timeout, the connect is aborted with a status message and no game process is launched; never a crash.

Apps are opened through their URI scheme (`steam://`, `discord://`) handed to the Windows shell via the same `explorer.exe` mechanism already used for `fivem://` (the launcher must not `ShellExecuteEx` the URI directly — FiveM's GTAVLauncher legit flow refused the GTA boot when it came from a launcher process; shell context is required, and it is the established, tested process seam). Nothing is opened when nothing is required or everything required is already running. RSC is never managed.

## User Stories

1. As a player connecting to a server that requires Steam while Steam is not running, I want the launcher to start Steam automatically, so that I don't have to start it manually and retry.
2. As a player, I want the launcher to wait until Steam is actually running before connecting, so that the server does not reject me for a missing Steam ticket.
3. As a player connecting to a server that requires Discord while Discord is not running, I want the launcher to start Discord and wait for it, so that the connection is not rejected by server-side Discord checks.
4. As a player, I want to see "Starting Steam..." / "Starting Discord..." while the launcher is starting a required app, so that I know what it is doing instead of staring at no feedback.
5. As a player, when a required app fails to start or never becomes running within a reasonable time, I want the connect aborted with a clear status message (not a launch, not a crash), so that I can decide what to do (start it myself, install it).
6. As a player, when everything that the server requires is already running, I want the connect to proceed exactly as today with no extra noise, so that the "don't bother if everything is ready" principle holds.
7. As a player connecting to a server with both Steam and Discord required, I want both prepared before connecting, so that a single connect succeeds without further interruption.

## Implementation Decisions

### Domain vocabulary
- "Preparing" an external app = ensure it is running (start if missing, then check readiness). The previous phase's readiness seam (`Service/IRequirementReadiness`) is the *checker*; this phase adds the *starter* and the *preparer* (the "StartupManager / Checker" responsibility named in DOC.md).

### New seam: `Launch/IExternalAppStarter`
- Contract: `Task StartAsync(ExternalApp app)`. Semantics mirror `IGameProcessLauncher` exactly: it launches the app's URI (`steam://` for Steam, `discord://` for Discord) through `explorer.exe` via the existing `IProcessStarter` seam, and throws `Win32Exception` when the scheme is unregistered (`IUriSchemeRegistration` seam) so callers can distinguish "failed to start".
- Real impl `Launch/ExternalAppStarter` reuses `IProcessStarter` + `IUriSchemeRegistration`; app→scheme mapping lives in one site (mirroring `ProcessReadinessChecker`'s app→process-name mapping — one mapping per concern).
- If, during the refactor step, `GameProcessLauncher` and `ExternalAppStarter` visibly duplicate the "check scheme + explorer.exe" logic, extract a small shared shell-URI launcher both delegate to (DRY); otherwise keep the duplication minimal and explicit.

### New application type: `Launch/ExternalAppPreparer`
- Contract: `Task<bool> TryPrepareAsync(ExternalApp app)`.
  - If the app is already running (via `IRequirementReadiness`) → `true`, no start, no wait.
  - Otherwise start it via `IExternalAppStarter` and poll `IRequirementReadiness` up to `maxAttempts` times with `pollInterval` between checks; `true` as soon as it is running.
  - `false` on timeout or when starting throws (a `Win32Exception` from an unregistered scheme). Never throws to the caller.
- Testability hook instead of a new seam: the "wait between polls" is an injectable `Func<Task>` (default `Task.Delay(pollInterval)`), and `maxAttempts`/`pollInterval` are injectable values — the same injectable-delegate style as `ProcessReadinessChecker`'s `Func<string,bool>`. Defaults: 500 ms interval, 120 attempts (~60 s), all in `Launch/`.
- Polling counts attempts, not wall-clock, so the loop is deterministic in tests without a clock.

### MainViewModel connect flow
- The current hard block ("Requires X (not running)" + return) is replaced by preparation:
  1. Resolve effective requirements (unchanged).
  2. `missing = required apps not running` (unchanged computation, reuse `IRequirementReadiness`).
  3. For each missing app, sequentially: set `StatusText = "Starting {app}..."`, then `TryPrepareAsync`; on `false` → `StatusText = "Could not start {app}"` and abort (no launch).
  4. All required apps ready → launch exactly as today ("Launching FiveM..." / "Opening {client}..." / "Launch failed").
- No extra status when nothing is missing (principle from the previous phase: no "Steam ✓" noise on success).
- Preparation applies to the connect flow only; the direct Open-client flow performs no requirement checks today and stays unchanged.

### Status vocabulary
- During: `Starting Steam...` / `Starting Discord...`.
- Give-up: `Could not start Steam` / `Could not start Discord`.
- English, reused by tests.

## Testing Decisions

- Only external behavior is tested (starting, waiting, giving up, status text); no test asserts internal loop mechanics.
- **`Launch/ExternalAppStarterTests`**: maps Steam→`steam://`, Discord→`discord://`; passes the URI to `explorer.exe` via the process-starter fake; throws `Win32Exception` when the scheme is unregistered. Prior art: `GameProcessLauncher` tests + `IProcessStarter`/`IUriSchemeRegistration` fakes already in the test project.
- **`Launch/ExternalAppPreparerTests`**: already running → `true` and never calls the starter; starts then becomes running → `true`; never becomes running → `false` after bounded attempts; starter throws → `false` (never throws); readiness transitions during the wait → `true` only after the transition. Fakes: `FakeRequirementReadiness` (existing), a fake `IExternalAppStarter`, `() => Task.CompletedTask` wait.
- **`ViewModels/MainViewModelTests`**: connect to a server requiring a missing app → "Starting Steam..." is set and the app is started, then the connect proceeds (launch result path); app never ready → "Could not start Steam" and no launch; both Steam and Discord required and missing → both prepared before launching; nothing missing → untouched current behavior.

## Out of Scope

- Locating and launching the app's executable from its install folder (registry/known paths) as a fallback when the URI scheme is unregistered — noted for a future phase; an unregistered scheme today degrades to "could not start".
- Distinguishing "running" from "ready"/"authenticated" — readiness stays the coarse process check (the seam keeps its name for that future sharpening).
- A progress bar / splash screen for preparation (single status line only).
- Preparing apps for the direct "Open FiveM" flow (no requirement checks exist there today).
- RSC is never managed.

## Further Notes

- Schema/JSON: no changes; `SavedServer`, `ServerRequirements`, repository, and `Requests` are untouched.
- The composition root (`App.xaml.cs`) gains the `ExternalAppStarter` and `ExternalAppPreparer` instances and passes them to `MainViewModel`.
- DOC.md responsibilities map: this phase delivers the "StartupManager / Checker" slice.