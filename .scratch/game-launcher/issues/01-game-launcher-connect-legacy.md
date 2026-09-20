# 01: GameLauncher lanza fivem://connect para un perfil Legacy con CfxId

**What to build:** primera slice vertical del orquestador. `GameLauncher.ConnectAsync(profile)` con un perfil Legacy (CfxId + Requirements) delega `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1` al seam `IGameProcessLauncher` y devuelve `LaunchResult.Connect(uri)`. Incluye definir `LaunchResult` (inmutable) y el seam `IGameProcessLauncher` + fake de test.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `IGameProcessLauncher` con `StartAsync(Uri)`; fake en tests (`FakeGameProcessLauncher`) que registra requests.
- [x] `LaunchResult` inmutable con payload; factories explícitas (sin ctor público).
- [x] `GameLauncher.ConnectAsync` con perfil Legacy+CfxId+Requirements → seam recibe `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1`.
- [x] Resultado `Connect(uri)` devuelto.
- [x] Suite completa verde.

## Comments

Nota: la capa se nombró `Launch` (namespace `FiveMServerLauncher.Launch`) en vez de `Application` porque chocaba con `System.Windows.Application` (WPF, `App.xaml.cs`).