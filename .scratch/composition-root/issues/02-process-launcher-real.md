# 02: GameProcessLauncher real (proceso via shell-execute URI)

**What to build:** `IGameProcessLauncher` producing real process launch. Implementación simple: `Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true })` — shell-execute del protocolo registrado (fivem://). Sin tests reales.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `Launch/GameProcessLauncher.cs` con `StartAsync(Uri) → Process.Start(...UseShellExecute = true)`.
- [ ] Compila. Suite verde.

## Comments