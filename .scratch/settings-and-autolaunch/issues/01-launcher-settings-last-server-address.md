# 01: LauncherSettings gains LastServerAddress

**What to build:** the settings model remembers the last-used server address so auto-launch has
something to point at. `LauncherSettings` gains a nullable `LastServerAddress` (default `null`);
saving/loading round-trips it, and a JSON settings file written before the field existed still
loads cleanly (defaults). `PreferredClient` and `AutoLaunch` remain untouched.

**Blocked by:** none (can start immediately).

**Status:** resolved

- [x] `LauncherSettings` exposes `string? LastServerAddress`, default null.
- [x] Storage round-trip: save+load preserves the address; a pre-existing file lacking the field
      deserializes with `null`.
- [x] `ConfigurationRepository.Load` returns defaults when storage yields null.
