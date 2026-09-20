# 01: GameLauncher devuelve StartFailed cuando el proceso falla

**What to build:** `IGameProcessLauncher.StartAsync` puede lanzar excepciones (por ejemplo si `fivem://` no está registrado). Hoy, esas excepciones suben al `GameLauncher`, rompen el flujo async y jamás llegan al VM. Tests RED: fake con `StartAsync` lleno de `Win32Exception` → `LaunchResult.StartFailed` verify/marca. [] 

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `LaunchResult.StartFailed` (sealed record sin estado).
- [x] `GameLauncher.ConnectAsync` intenta `await _processLauncher.StartAsync(uri)`, captura la excepción y devuelve `LaunchResult.StartFailed`.
- [x] Fake throws exception fixture en tests (`FakeGameProcessLauncher.ThrowOnStart`).
- [x] Suite verde.

## Comments