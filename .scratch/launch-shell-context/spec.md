Status: resolved
Type: spec

# Launch FiveM from Windows shell context (Explorer-shell routing)

## Problem Statement

Connecting from the launcher reaches FiveM with the **correct** `fivem://connect/...` URI (visible in `CitizenFX_*.log` — same URI that connects when pasted in Win+R), FiveM starts and RSC validation passes, but the final GTA client never opens. GTAVLauncher refuses the GTA boot with `This application should be launched directly from the shell or a web browser.` The identical bare URI launched via Win+R or PowerShell `Start-Process` always connects.

Earlier suspicion: a pending Rockstar GTA V update (3258 → 3751 → 3889). That was a confound: when the update settled, Win+R with the bare URI worked while the launcher with the same URI still failed. The remaining variable was the launch context of the launcher process itself.

## Solution

The real process launcher stops performing the URL launch itself (`Process.Start(uri, UseShellExecute=true)` → `ShellExecuteEx` from the launcher process). Instead:

1. It verifies the `fivem://` protocol is registered, and if not throws the same `Win32Exception` the old path produced — so `GameLauncher` still returns `LaunchResult.StartFailed` (the `process-launch-failure` contract survives).
2. It hands the URI to **explorer.exe** via plain `CreateProcess` (`ProcessStartInfo { FileName = "explorer.exe", Arguments = uri.AbsoluteUri, UseShellExecute = false }`). The always-running Explorer shell routes the `fivem://` URI exactly as Win+R does, so FiveM is spawned from genuine shell context. Because Explorer always runs at medium integrity, this also survives an elevated launcher.

No URI, address, flags, RSC, or `FiveMLaunchOptions.ToUri()` behaviour changed — they are all exonerated by the diagnosis.

## User Stories

1. As a player, I want to press connect in the launcher and have the game client open and connect, so that I can play without manually copying the URI into Run.
2. As a player, I want the launcher result to be identical to pasting the URI in Win+R, so that GTAVLauncher never refuses the boot just because I used a launcher.
3. As a developer, I want the real process launch to be testable at a seam without real processes, so tests stay fast and the launch policy is pinned.
4. As a developer, I want `LaunchResult.StartFailed` to still be reachable when the `fivem://` protocol is unregistered, so the documented error contract is preserved.
5. As a developer, I want the launch to keep working even when the launcher runs elevated, so I don't depend on the launcher's integrity level.
6. As a developer, I want the launch policy independent of which process invoked the launcher, so shell context is the only thing FiveM sees.

## Implementation Decisions

- **Seams.** Existing seam `IGameProcessLauncher.StartAsync(Uri)` remains the application contract; `GameLauncher.ConnectAsync` and `LaunchResult` (`Connect`/`OpenClient`/`StartFailed`, no null-union) are untouched. The real `GameProcessLauncher` is tested through two new `Launch/` seams:
  - `IProcessStarter` / `ProcessStarter` — wraps the static `Process.Start(ProcessStartInfo)` (the real implementation was previously the only untested production class touching a static).
  - `IUriSchemeRegistration` / `UriSchemeRegistration` — `IsSchemeRegistered(scheme)` reading `Registry.ClassesRoot` (`fivem` class key presence).
- **Launch policy** (`GameProcessLauncher.StartAsync`): throw `Win32Exception("No application is associated with the specified file.")` when `IsSchemeRegistered(uri.Scheme)` is false; otherwise start `explorer.exe` with the absolute URI as its argument and return immediately (never wait for FiveM — "process started" is not "connected").
- **Why explorer.exe and not a direct `ShellExecuteEx`:** the direct call from the launcher process made GTAVLauncher's legit flow refuse the boot. `explorer.exe` restores the shell/browser launch origin that FiveM and RGL expect.
- **Composition root** wires `GameProcessLauncher(new ProcessStarter(), new UriSchemeRegistration())`.
- AGENTS.md `Launch/` and composition-root sections were updated to describe the real launch (they previously said real process start was out of scope).

## Testing Decisions

- Test at the real `GameProcessLauncher` seam, mirroring the repo's fake family (`FakeGameProcessLauncher`, `FakeDnsResolver`, `FakeHttpMessageHandler`): `FakeProcessStarter` records `ProcessStartInfo`; `FakeUriSchemeRegistration` returns a configurable answer.
- Good test = external behaviour through the seam, not static calls: the code under test is the decision "throw when unregistered, else hand the URI to the shell".
- Assertions: exactly one `Start`; `FileName == "explorer.exe"`; `Arguments == uri.AbsoluteUri`; unregistered scheme ⇒ `Win32Exception` with the standard message. **Not** asserted: `UseShellExecute` (implementation detail of how Explorer is spawned).
- Prior art: `GameLauncherTests` already covers `StartFailed` via `FakeGameProcessLauncher { ThrowOnStart = true }`; naming `<Method>_Should<Expectation>` with Given/When/Then.
- Full suite: `dotnet test` green at 119 tests.

## Out of Scope

- Detecting Rockstar GTA update state (diagnosed as a confound; no launcher-side check added).
- Monitoring the spawned process or confirming the game reached the server ("process started" ≠ "service ready/authenticated").
- Alternative mechanisms (launching FiveM by executable path, localhost redirect, changing `FiveMLaunchOptions.ToUri()` output).
- Any change to address resolution, CFX vars, or the `LaunchResult` contract.

## Further Notes

- Diagnosis used FiveM logs (`CitizenFX_log_*.log`): a working run starts with `hello from "D:\My Games\FiveM\FiveM.exe" "fivem://connect/..."`, and the failures — while the launcher delivered the identical bare URI — ended at GTAVLauncher's legit refusal with no `b<build>_GTAProce` line.
- Manual validation before implementing: `explorer.exe "fivem://connect/cfx.re/join/y4lg95"` and `Start-Process "fivem://connect/cfx.re/join/y4lg95"` from PowerShell both fully connect; the C# `ShellExecuteEx` did not.
- Implemented and committed as `9c65106 fix(launch): hand FiveM connect URI to the Explorer shell`; recorded as a resolved spec per the workflow.