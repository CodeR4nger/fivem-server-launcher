Status: resolved
Type: spec

# PathResolver: detect FiveM installations by real path

## Problem Statement

Today `GameLauncher.ConnectAsync` can return `OpenClient(GameClient)` or `StartFailed`, but there is no way to distinguish "couldn't connect" from "the client is not installed". DOC.md mentions `PathResolver` as an Infrastructure responsibility; there is no seam or implementation.

The real install path (researched/documented):
- **Legacy (FiveM)**: `%localappdata%\FiveM\FiveM.app\FiveM.exe` (official FiveM docs).
- **Enhanced**: `%localappdata%\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe` — exact path confirmed by the user (it installs into that folder by default).

## Solution

- `Service/IClientInstallLocator` (seam): two injectable methods, `bool IsInstalled(GameClient)` and `string? GetExecutablePath(GameClient)`. Never null for IsInstalled; null only for the executable.
- `Service/ClientInstallLocator` (real): `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` + `Path.Combine(base path).\FiveM.app\FiveM.exe` (Legacy) or `base path\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe` (Enhanced).
- No real filesystem tests (repo fake-seam tests are used).

## User Stories

1. As a player, if Enhanced is not installed, the launcher will tell me there is no escalation.
2. As a developer, I want to test detection without touching the filesystem (fake seam).

## Implementation Decisions

- `IClientInstallLocator`: `Task<bool> IsInstalled(GameClient)` and `Task<string?> GetExecutablePath(GameClient)` — async-ready (symmetry with Service seams).
- Real `ClientInstallLocator` uses `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "FiveM.exe")` for Legacy, and `Path.Combine(..., "FiveM for GTAV Enhanced", "FiveM.app", "FiveM.exe")` for Enhanced.
- There is no pure public path probing (all `Path.Combine` are internal).
- Fake `IClientInstallLocator` tests so tests don't touch the OS.

## Testing Decisions

- Tests on the seam (`FakeClientInstallLocator`, configurable installs).
- Unit tests around `GetExecutablePath` and `IsInstalled` of `ClientInstallLocator` (using `Path.Combine` plus local stubs). This is done flat with the filesystem-free test model.
- Fully green suite at the end.

## Out of Scope

- Splash / reboot loop, job courtesy, "installed vs ready/not-authed" state.
- Detection of custom folders or portable installs (`GetFolderPath` doesn't detect them; we cover the default).
