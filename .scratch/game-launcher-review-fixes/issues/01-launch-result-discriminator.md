# 01: LaunchResult con discriminador explícito (sin null-union)

**What to build:** `LaunchResult` pasa de una clase con dos payloads nullable a una jerarquía de records sealed: `Connect(Uri) : LaunchResult` y `OpenClient(GameClient) : LaunchResult`. El discriminador es el tipo (pattern matching). Se mantiene inmutable, sin ctor público sobre la base; instanciación vía factories estáticas finales. Los tests de `GameLauncher` se adaptan para assert sobre la jerarquía (junto al cambio, se renombra el test malformed "ValidationCfx").

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `LaunchResult` es jerarquía sealed (`Connect(Uri)` / `OpenClient(GameClient)`); sin ctor público, sin null-union.
- [x] `GameLauncher.ConnectAsync` devuelve `new LaunchResult.Connect(uri)` o `new LaunchResult.OpenClient(profile.GameClient ?? GameClient.FiveM)`; compilación intacta.
- [x] Tests de `GameLauncher` usan pattern matching / `IsType` sobre el resultado (sin assertions genéricas sobre null-union).
- [x] Se renombra `ConnectAsync_WithValidationCfxProfile_ShouldLaunchConnectUri` → `WithValidatedCfxProfile`.
- [x] Suite completa verde (106).

## Comments