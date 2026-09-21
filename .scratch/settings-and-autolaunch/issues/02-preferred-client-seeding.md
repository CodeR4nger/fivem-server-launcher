# 02: VM reads settings: preferred-client seeding + save-on-change

**What to build:** `MainViewModel` consumes the settings seam so the launcher finally respects the
user's preferences. The OPEN dropdown's initial selection comes from the persisted preferred client
when that client is installed, falling back to the first installed client otherwise (matching the
current default behaviour when nothing is installed). Changing the preferred client or the
auto-launch toggle in the settings state immediately persists through `ConfigurationRepository` —
a second launch sees the new values.

**Blocked by:** 01 (LauncherSettings gains LastServerAddress).

**Status:** resolved

- [x] `MainViewModel` receives `ConfigurationRepository` in its ctor; composition root knows about
      it (`launcher-settings.json` beside the other app data files).
- [x] `InitializeAsync` seeds `SelectedOpenClient` from the persisted `PreferredClient` when
      installed, otherwise first-installed; persisted-but-absent client never lands as selection.
- [x] `AutoLaunch` and `PreferredClientOption` setters immediately persist via the repository,
      merging over the loaded settings (`LastServerAddress` is preserved — verified by test).
