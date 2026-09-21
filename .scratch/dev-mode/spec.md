# Dev Mode

**Status:** ready-for-agent

## Problem Statement

`FiveMLaunchOptions` already models `GameBuild`, `PureMode` and `SecondClient` (`-cl2`) and knows
how to serialize them (`-b<build> -pure_<nivel> [-cl2]` via `ToCommandLineArgs()`), but **nothing in
the product exposes them**. A player who wants to open FiveM with a manual game build / pure mode /
second client has no way to do it from the launcher.

## Solution

A **Dev Mode toggle** on the main window swaps the FiveM panel into a dev view: a Game Build field,
a Pure Mode selector (0/1/2), a primary LAUNCH button and a secondary **with `-cl2`** button
(second client is an action, not a setting — per user, it shows as its own button and is never
persisted). Game Build and Pure Mode **persist** in `LauncherSettings`; second client does not.
Dev Mode is **open-directly only** (Legacy FiveM): the dev options never touch server connections,
where server-published requirements always win. Launching with flags goes through the `fivem://`
scheme with the flags as the URI payload (`fivem://-cl2` works — user-verified), so no executable
resolution is needed for the dev launch.

## User Stories

1. As a developer player, I want a DEV MODE toggle on the main window, so that dev tools stay
   separate from the normal experience.
2. As a developer player, I want to type a Game Build in Dev Mode and have it persist, so that I
   don't retype it every session.
3. As a developer player, I want to pick Pure Mode (0/1/2) in Dev Mode and have it persist.
4. As a developer player, I want a primary LAUNCH button that opens FiveM Legacy with my dev flags
   and no `SecondClient`, so normal dev launch stays one click.
5. As a developer player, I want a secondary button that launches with `-cl2`, so I can open a
   second client without persisting a flag.
6. As a player, I want the dev options to never affect server connections, so server-published
   requirements always win.
7. As a player, I want Dev Mode to be unavailable/hidden for FiveM Enhanced, since it doesn't
   support `-b`/`-pure_`/`-cl2` (verified in DOC.md).
8. As a player, I want a way back to the normal panel.

## Implementation Decisions

- **`LauncherSettings`** gains `int? DevGameBuild` and `int? DevPureMode` (nullable, default null).
  No SecondClient field. Persistence via the existing storage/repo; a missing field loads as null
  (same tolerance as `LastServerAddress`).
- **Serialization stays on `FiveMLaunchOptions.ToCommandLineArgs()`** (`-b<build> -pure_<n> -cl2`,
  empty for Enhanced). No URI serializer is added: user re-verified the `fivem://<flag>` route only
  carries a *single* parameter, so it is not viable for combined flags.
- **Launch route: exe + CLI args through the shell.** `IGameProcessLauncher.StartExecutableAsync`
  gains an args parameter (or an overload). The real implementation starts the resolved
  `FiveM.exe` with `ProcessStartInfo { FileName = path, Arguments = <args>, UseShellExecute = true }`
  — the ShellExecute route is explorer/shortcut-equivalent (shell context, so the Rockstar check
  passes, unlike the raw `CreateProcess` route rejected in phase 1), and unlike the `fivem://` URI
  it forwards arguments. The connect flow's URI path is untouched.
- **Application seam:** `GameLauncher.OpenAsync(FiveMLaunchOptions options)` — resolves the legacy
  exe via `IClientInstallLocator`, builds args from `ToCommandLineArgs()`, starts via the process
  seam; returns `OpenClient(client)` on success, `NotInstalled(client)` when the exe can't be
  resolved, `StartFailed` on any throw. Enhanced options (`ToCommandLineArgs` empty anyway) still
  go through the same path (a plain `fivem://`-less open), since Enhanced has no flags.
- **VM:** `IsDevMode` (session-only) toggles the panel; `DevGameBuild` (string, digits) and
  `DevPureMode` (int? 0/1/2) persist through the settings repository on change; `DevLaunchCommand`
  takes a bool command parameter (null/false = plain, true = `-cl2`). Status via existing `Describe`.
- **XAML:** a "DEV MODE" button toggles `IsDevMode`; the left panel swaps between the normal OPEN
  area and the dev controls (converter, no code-behind).
- Dev Mode always opens Legacy FiveM (Enhanced gets no flags — direct open already exists for it).

## Testing Decisions

- External behaviour only; existing fakes/seams.
- `LauncherSettingsTests`/`FileSettingsStorageTests`: dev fields round-trip + missing-field → null.
- `FiveMLaunchOptionsTests`: `ToCommandLineArgs` already covers serialization — no new domain
  serializer needed.
- `GameLauncherTests`: `OpenAsync(options)` happy / `NotInstalled` / `StartFailed` paths with
  `FakeGameProcessLauncher` (fake records exe starts **with args**).
- `MainViewModelTests`: dev toggle, persistence of build/pure via repository, command parameter
  drives `-cl2`, status mapping.

## Out of Scope

- RedM open (phase 4), UI polish (phase 5), auto-update, richer server-info UI.
- Dev overrides on server connect (never; server wins).
- Persisting IsDevMode or SecondClient.

## Further Notes

User decisions: dev panel = main-panel toggle; build/pure persist, second client as a secondary
button (not persisted); launch route = exe + CLI args through the shell (`UseShellExecute = true`)
after the `fivem://<flag>` URI route turned out to accept only a single parameter.
