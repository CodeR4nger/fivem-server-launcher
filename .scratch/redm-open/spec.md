# RedM direct-open

**Status:** ready-for-agent

## Problem Statement

RedM is already a first-class *connect* target (CFX `gamename = rdr3` maps to `GameClient.RedM`,
`fivem://connect` happens for it), but: (a) the OPEN dropdown never offers RedM, (b) the connect URI
is hardcoded to `fivem://` even for RedM servers, and (c) `ClientInstallLocator` doesn't probe the
red-m install. A RedM player can laboriously connect via a `fivem://` URI (never validated) but
cannot open RedM from the launcher or hit the correct `redm://connect` scheme.

## Solution

- `ClientInstallLocator` probes RedM: registry `HKCU\Software\CitizenFX\RedM` → `Last Run Location`
  (mirroring Legacy), with the default path `%localappdata%\RedM\RedM.app\RedM.exe` as fallback.
- `FiveMLaunchOptions.ToUri()` picks the scheme per client: `redm://connect/...` for RedM,
  `fivem://connect/...` for Legacy/Enhanced. Enhanced still returns null (no URI).
- The OPEN dropdown lists RedM when installed (same installed-only behaviour as the others).
  Dev Mode stays Legacy-only (no RedM `-cl2`/build/pure path exposed — RedM CLI flags exist in
  DOC.md but Dev Mode is a FiveM option per user).

## User Stories

1. As a RedM player, I want RedM listed in the OPEN dropdown when installed, so I can open it like
   the FiveM clients.
2. As a RedM player, I want `redm://connect/...` used when connecting to an rdr3 server, so the
   right protocol handler picks it up.
3. As a RedM player with servers published on CFX, I want the `gamename = rdr3` profile to flow
   automatically into RedM open/connect paths without extra config.
4. As a player without RedM installed, I want RedM absent from the dropdown, so it can't be
   "opened" to nothing.
5. As a developer, I want the install-locator seam to stay injectable so tests cover RedM resolution
   without a real install.
6. As a RedM player, I want connect-with-flags (`-b`/`-pure_`) to keep working since DOC.md
   documents that Legacy and RedM support them.
7. As a player, I want the dropdown label "RedM" for the RedM option.
8. As a player, I want preferred client = RedM in settings to seed the dropdown when installed.

## Implementation Decisions

- **`ClientInstallLocator`:** new `ResolveRedM` branch — registry read via the same injectable
  provider seam pattern already used for `FiveM` Last Run Location (extend the existing
  `_legacyInstallDirectoryProvider`-style seam: add a second provider or generalize to one
  `(GameClient)`-keyed provider; pick the simplest, tests decide).
- **`FiveMLaunchOptions.ToUri()`:** scheme by `GameClient`: `redm` when `RedM`; `fivem` otherwise
  (Enhanced returns null — unchanged). Query string unchanged (`-b`/`-pure_` only).
- **`MainViewModel`:** `OpenCandidates` gains `GameClient.RedM`; seeding/fallback unchanged.
- **Validation:** DevMode remains Legacy-only; `ToCommandLineArgs()` keeps working for RedM but is
  not surfaced in Dev Mode.
- No new `LaunchResult` types; `OpenAsync(GameClient.RedM)` reuses `OpenCoreAsync`.

## Testing Decisions

- `ClientInstallLocatorTests` (existing seam-based tests): registry-first, fallback default path,
  not-installed → null for both RedM probes.
- `FiveMLaunchOptionsTests`: `redm://connect/cfx.re/join/<id>` for RedM, scheme unchanged for
  Legacy, Enhanced still null.
- `MainViewModelTests`: RedM listed when installed, not when absent; open command starts the RedM
  exe path; preferred-client seeding honours RedM.

## Out of Scope

- RedM in Dev Mode (keep Legacy-only).
- RedM connect validation UX beyond the existing forms.

## Further Notes

User decisions (with confirmations): `redm://connect` scheme for RedM; default RedM path +
registry `Last Run Location` probing; DOC.md flags apply to RedM.
