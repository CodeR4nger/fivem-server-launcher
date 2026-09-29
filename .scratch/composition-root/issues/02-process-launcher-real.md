# 02: Real GameProcessLauncher (process via shell-execute URI)

**What to build:** `IGameProcessLauncher` producing a real process launch. Simple implementation: `Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true })` — shell-execute of the registered protocol (fivem://). No real tests.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Launch/GameProcessLauncher.cs` with `StartAsync(Uri) → Process.Start(...UseShellExecute = true)` (later refined to the `explorer.exe` shell route via `UriShellStarter`, commit `9c65106` — shell context required by FiveM's launcher check).
- [x] Compiles. Suite green.

## Comments
