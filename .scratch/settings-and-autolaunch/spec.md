# Settings + auto-launch

**Status:** ready-for-agent

## Problem Statement

`LauncherSettings` (`PreferredClient`, `AutoLaunch`) and its persistence chain
(`ISettingsStorage` → `FileSettingsStorage` → `ConfigurationRepository`) exist since the beginning
but **nothing reads them**: the OPEN dropdown always defaults to the first installed client, there is
no settings UI, and no auto-launch behavior. A player who plays on the same community every day must
retype/repick the server address on every launch even if they enabled "auto-launch" in their head —
they simply cannot, because the toggle does not exist.

## Solution

- A small settings surface (gear/⚙ button toggling an inline panel) with exactly two controls:
  a preferred-client selector and an auto-launch checkbox. Changes save immediately through the
  existing `ConfigurationRepository`.
- `PreferredClient` seeds the OPEN dropdown default selection at startup (when installed).
- The launcher remembers the **last successfully used** server address (a new `LastServerAddress`
  field on `LauncherSettings`, confirmed with the user as being a launcher-level preference, not a
  per-server field). When `AutoLaunch` is on and a last address exists, the launcher auto-fills the
  connect box with it and auto-connects right after startup.

## User Stories

1. As a player, I want to pick my preferred client (FiveM / FiveM Enhanced) in settings, so that the
   OPEN button defaults to the client I actually use.
2. As a player, I want my preferred client used only when that client is installed, so that I never
   land on an unlaunchable selection.
3. As a player, I want an "auto-launch" toggle in settings, so that the launcher can reconnect to my
   usual server without me typing the address again.
4. As a player with auto-launch enabled, I want the launcher to fill the address box with my last
   server and connect automatically at startup, so that I reach the game faster.
5. As a player, I want the remembered server updated after each successful connect, so that
   auto-launch always targets the server I actually used last.
6. As a player, I want settings changes to persist immediately on change, so that closing the
   launcher right after toggling does not lose my choices.
7. As a player whose settings file is missing or corrupt, I want sane defaults (FiveM preferred, no
   auto-launch, no remembered address), so that the launcher always starts.
8. As a developer, I want `LauncherSettings` to stay global-only (no per-server fields beyond the
   remembered single address), so that the DOC.md boundary stays intact.
9. As a developer, I want auto-launch to reuse the exact same `ConnectAsync` flow (including
   preparation of required apps), so that a manual and an auto connect behave identically.
10. As a player, I want auto-launch to silently skip when the remembered address is invalid or the
    network/catalog is unreachable, falling back to the normal manual flow, so that a broken network
    never traps me in an error state.
11. As a developer, I want `PreferredClient` seeding and auto-launch to live in `MainViewModel` behind
    the existing seams, so that they are fully unit-tested without a real UI.

## Implementation Decisions

- **`LauncherSettings`** gains `string? LastServerAddress` (nullable, default `null`). `PreferredClient`
  (default `GameClient.FiveM`) and `AutoLaunch` (default `false`) stay untouched. JSON serialization
  via the existing `JsonStringEnumConverter` options; unknown/absent fields deserialize to defaults.
- **Settings surface = view model + inline panel in `MainView.xaml`**:
  - `MainViewModel` gains `SettingsCommand` (toggles `IsSettingsOpen`), `AvailableOpenClients` is
    reused as the preferred-client option list (installed clients only), `PreferredClient`
    (an `InstalledClientOption?` bound to the settings selector), and `AutoLaunch` (bool) — the
    last two save on change by `Save(Repository.Load() merged)`.
  - XAML: a gear/`⚙` `Button` commanding `SettingsCommand`; a `Border` panel whose `Visibility`
    binds to `IsSettingsOpen` (converter-less: use a boolean→visibility converter already? none
    exists — simplest is to bind `Visibility` via a `BooleanToVisibilityConverter` resource;
    or expose the panel `Visibility` from the VM - decide in implement; KISS favours the standard
    WPF converter).
- **`ConfigurationRepository` is injected into `MainViewModel`** (new ctor param, wired in
  `App.xaml.cs` from `FileSettingsStorage` in `%localappdata%\FiveMServerLauncher\launcher-settings.json`
  — single real construction point; shared DOC hint is "settings file beside saved-servers.json").
- **Startup sequence (in `InitializeAsync`)**:
  1. Load settings via repository (never null after `?? new LauncherSettings()` inside the repo).
  2. Seed `SelectedOpenClient`: pick `PreferredClient` when installed, else first installed, else
     none (covered when list empty).
  3. If `AutoLaunch && !string.IsNullOrWhiteSpace(LastServerAddress)`: set `ServerAddress` to it and
     `await ConnectAsync()` (same command path as a manual connect: resolve → prepare requirements →
     launch).
- **Remembering the server**: in `ConnectAsync`, after a successful `LaunchResult.Connect` or
  `OpenClient` (both mean "the game client will launch"), persist `LastServerAddress = ServerAddress`
  (trimmed) via repository save. `StartFailed`/`NotInstalled`/invalid-address do not clobber the
  remembered value.
- **Failure containment**: an exception during auto-launch connect is swallowed into a status line
  (same strings as manual connect); the window stays usable. An invalid remembered address surfaces
  the existing "Invalid address" status — the user can simply type a new one.
- **Tests seam**: `MainViewModel` ctor gains `ConfigurationRepository`; tests use
  `InMemorySettingsStorage` (existing fixture) — no filesystem in tests.
- `FiveMLaunchOptions`, `GameLauncher`, `ServerResolver`, `IClientInstallLocator` are unchanged.
- Settings panel styling: reuse existing button/border resources; final polish deferred to phase 5
  (UI improvement) per roadmap.

## Testing Decisions

- Test external behaviour only (session log: "only test external behavior").
- `LauncherSettingsTests`: extended for the new `LastServerAddress` default (Given/When/Then
  naming as already in these files).
- `FileSettingsStorageTests`: extended for `LastServerAddress` round-trip, and unknown-field
  tolerance (a settings JSON without the field deserializes to `null`); temp-file fixture
  `TempSettingsDirectory` reused.
- `MainViewModelTests` (heaviest): preferred-client seeding (installed preferred honoured; non-
  installed preferred falls back; empty list), auto-launch (connect fires with remembered address,
  skipped when off / when address absent), persistence of last address on successful connect (and
  not on failure), settings save-on-change for both preferences, and that settings gear toggles
  panel visibility.
- Existing 259-test suite stays green; no mocking library added.

## Out of Scope

- UI polish / final settings design (phase 5).
- Dev Mode (game build / pure / `-cl2`) settings — separate phase 3.
- RedM open option (phase 4).
- Auto-update of the launcher (independent item).
- Remembering anything per-server inside `LauncherSettings` (address *of last use* is a launcher
  preference, accepted by user; per-server fields remain out).

## Further Notes

- Confirmed with user: (a) last server address lives inside `LauncherSettings` JSON; (b) auto-launch
  performs a real auto-connect to the last server at startup; (c) settings UI = gear button + small
  inline panel.
