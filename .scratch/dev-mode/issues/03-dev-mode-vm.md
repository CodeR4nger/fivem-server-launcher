# 03: Dev Mode VM (toggle + persisted build/pure + launch command)

**What to build:** `MainViewModel` exposes `IsDevMode` (toggle command), `DevGameBuild` (string)
and `DevPureMode` (int?) bound properties that persist on change through the settings repository,
and a `DevLaunchCommand` that opens Legacy FiveM with the dev options; a bool command parameter
marks the `-cl2` secondary launch. Status goes through the shared `Describe` mapping.

**Blocked by:** 01, 02.

**Status:** resolved

- [ ] Toggle command flips `IsDevMode`.
- [ ] Build/pure changes persist via settings repository on change (merge, preserving other fields).
- [ ] Launch command builds `FiveMLaunchOptions` with the dev values + parameter-driven `SecondClient`
      and opens via `GameLauncher.OpenAsync(options)`.
- [ ] Status text uses the shared `Describe` mapping.
