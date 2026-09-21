# 02: Dev-open primitive (launcher + process seam with args)

**What to build:** `GameLauncher.OpenAsync(FiveMLaunchOptions)` exists: it resolves the Legacy exe
via the locator, serializes the args from `ToCommandLineArgs()`, and starts the executable with
those CLI args via the process seam (real impl: `ProcessStartInfo` with `UseShellExecute = true`),
reporting `OpenClient` / `NotInstalled` / `StartFailed`.

**Blocked by:** none.

**Status:** resolved

- [ ] `IGameProcessLauncher.StartExecutableAsync` accepts CLI arguments (overload or parameter).
- [ ] Real impl uses `UseShellExecute = true` so the shell starts FiveM with the flags.
- [ ] `GameLauncher.OpenAsync(FiveMLaunchOptions)` pins OpenClient/NotInstalled/StartFailed.
- [ ] Fake process launcher records started args for E2E-style assertions.
