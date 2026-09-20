# 02: Real GameProcessLauncher (process via shell-execute URI)

**What to build:** `IGameProcessLauncher` producing a real process launch. Simple implementation: `Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true })` — shell-execute of the registered protocol (fivem://). No real tests.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `Launch/GameProcessLauncher.cs` with `StartAsync(Uri) → Process.Start(...UseShellExecute = true)`.
- [ ] Compiles. Suite green.

## Comments
