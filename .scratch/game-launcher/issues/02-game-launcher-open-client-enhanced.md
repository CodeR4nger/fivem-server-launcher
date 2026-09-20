# 02: GameLauncher devuelve OpenClient para FiveMEnhanced sin llamar al seam

**What to build:** perfil con `GameClient = FiveMEnhanced` no produce URI (`ToUri()` → null). `GameLauncher.ConnectAsync` debe devolver `LaunchResult.OpenClient(FiveMEnhanced)` **sin** llamar a `IGameProcessLauncher` (es la UI quien abre el cliente después). El miss de la slice 01 es que `ConnectAsync` delega sin validar el caso Enhanced.

**Blocked by:** 01

**Status:** resolved

- [x] `ConnectAsync` con perfil Enhanced → `LaunchResult.OpenClient(GameClient.FiveMEnhanced)`.
- [x] El fake registra 0 llamadas al seam en este caso.
- [x] Suite completa verde.

## Comments