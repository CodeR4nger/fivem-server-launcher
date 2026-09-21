Status: ready-for-agent
Type: spec

# Requirement readiness: wait for Steam to be fully loaded

## Problem

`ExternalAppPreparer` treats an external app as ready as soon as its **process exists**. For Steam
this is far too early: Steam boots in phases ("starting" -> "starting session" -> "fully opens"),
and even a remembered/auto-login user can sit on a slow session restore. Launching FiveM while
Steam is still coming up means the game lands before Steam can hand it a valid auth session.
`ProcessReadinessChecker` conflates "process started" with "service ready/authenticated", which
`AGENTS.md` and `DOC.md` explicitly forbid equating.

The connect flow also drops the ball: `MainViewModel.FindMissingRequirementsAsync` skips the whole
preparation when the app *process* is already running — so Steam running but not yet logged in is
never waited on. This is exactly the reported symptom: Steam is up, FiveM launches, cut to a
Steam login screen.

Verified against the real machine: a `steam` process can be present while
`HKCU\Software\Valve\Steam\ActiveProcess\ActiveUser == 0` (no authenticated user), proving the
coarse process probe is insufficient.

## Research conclusion

The documented Steamworks authentication APIs (session tickets, Web API `AuthenticateUserTicket`,
encrypted app tickets, OpenID) authenticate a user's *identity* between game clients, servers, and
backends — they are FiveM's job (`requestSteamTicket`), not the launcher's. The only true
"logged on" client-side signal is `ISteamUser::LoggedOn()` via `SteamAPI_Init()`, which would make
the launcher a Steamworks client (native `steam_api64.dll`, AppID, NuGet, init can fail for
non-Steam AppIDs). There is no lightweight documented API; a composite external probe is the right
answer.

## Solution

### 1. Ready vs running (seam)

`IRequirementReadiness` grows `Task<bool> IsReadyAsync(ExternalApp app)`, distinct from
`IsRunningAsync`:

- `IsRunningAsync` stays the coarse "process exists" gate (still used by nothing after this phase
  except internally).
- `IsReadyAsync` is the gate the preparer and the connect flow actually wait on:
  - **Steam**: ready iff `steam` process **and** `steamwebhelper` process are running **and** the
    Steam session user id from the registry is non-zero. The three probes map to the user's three
    startup phases: bootstrapper (`steam.exe`), UI web host up (`steamwebhelper.exe`), authenticated
    session (`HKCU\Software\Valve\Steam\ActiveProcess\ActiveUser != 0`). The login screen keeps the
    web helper up but `ActiveUser == 0`, so it stays not-ready; a fully opened Steam that is
    minimized to the tray is ready.
  - **Discord**: same as running (no stronger signal; out of scope this phase).
  - Unknown/other: false.
  - Any probe throwing or absent: false. `IsReadyAsync` never throws.
- The registry read is the same seam pattern as `ClientInstallLocator`:
  `Func<int?>` defaulting to `Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess", "ActiveUser", null)`.
  The two process probes reuse the existing `Func<string, bool>` seam (both `"steam"` and
  `"steamwebhelper"` are passed through it).

### 2. Preparer waits for ready

`ExternalAppPreparer.TryPrepareAsync` becomes:

1. Already ready -> `true`, nothing touched (don't bother).
2. Not running -> start it.
3. Poll `IsReadyAsync` until ready, then `true`; give up after `maxAttempts` with `false`.
   - Already-running-but-not-ready (e.g. Steam at the login screen) is **waited on without
     restarting** — this closes the reported hole.
4. Never throws (whole-body try/catch, unchanged).

### 3. Connect flow prepares every required app

`MainViewModel` drops `FindMissingRequirementsAsync` and the `IRequirementReadiness` dependency:
every required app (`SteamRequired == true`, `DiscordRequired == true`) goes through
`_preparer.TryPrepareAsync`, which self-returns `true` when nothing is needed — so the UX stays
"nothing extra when all are ready". Copy is unchanged: "Starting {app}..." while preparing,
"Could not start {app}" (no launch) on failure.

## Out of scope

- Discord "ready" beyond process presence.
- Waiting for the Steam **main window** (tray/minimize makes window probes flaky).
- RSC, Web API, OpenID, session tickets — FiveM owns game-side auth.
- Restarting a running-but-stuck Steam (the timeout surfaces "Could not start {app}" instead).

## Tests / TDD

TDD at the seams, RED -> GREEN -> REFACTOR, one vertical slice per ticket (01 -> 02 -> 03). No
real processes, no real registry, no HTTP in unit tests; credit/adapt the existing
`ProcessReadinessChecker` fakes (`Func<string,bool>`), the registry seam, and the
`StatefulRequirementReadiness`/`FakeRequirementReadiness` fake upgrade to expose a `ready` axis.
End-state: the suite is green and `dotnet test` passes; code-review (Standards + Spec) applied
before commit.

## Docs to update (same commit)

- `DOC.md`: readiness paragraph — process-running is the coarse gate; Steam readiness is the
  composite probe (helper + authenticated session).
- `AGENTS.md`: `IRequirementReadiness`/`ProcessReadinessChecker` bullet (composite Steam probe,
  registry seam), `ViewModels` bullet (prepare every required app, no missing-gate), composition
  root note (readiness no longer passed to the VM).
- This feature is the documented "later phase can sharpen 'running' into 'ready'/authenticated".