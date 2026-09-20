# 01: FiveMLaunchOptions de record a sealed class (invariante en compilación)

**What to build:** `src/FiveMServerLauncher/Domain/FiveMLaunchOptions.cs` pasa de `sealed record` a `sealed class` con ctor privado y get-only props (sin `init`), eliminando el `with` y bloqueando en compilación cualquier estado inválido fuera de `Create`. `ToUri()`/`ToCommandLineArgs()`/`IsEnhanced`/`BuildGameFlags`/`ValidateAddress`/`ValidateGameClient` se conservan sin cambiar su comportamiento. `Create` y `FromServerProfile` siguen siendo las únicas fábricas públicas.

**Blocked by:** None

**Status:** resolved

- [x] `FiveMLaunchOptions` es `sealed class` con ctor privado y get-only props (sin `init`), y no existe método `with`/clone sintético — verificable por reflexión
- [x] No hay setter público de ningún tipo (ni `init`, ni `set`) en las props — reflexión lo confirma
- [x] `Create`/`FromServerProfile` siguen siendo las únicas vías públicas de construcción
- [x] `ToUri()` aún devuelve `null` con `Address` nula o `GameClient == FiveMEnhanced`, y URI/args con el formato exacto de hoy (sin cambios de comportamiento)
- [x] `Value_ShouldBeImmutableAfterConstruction` se adapta (sin `with`): verifica ausencia de mutadores públicos y no-identidad de instancias
- [x] Suite completa verde (61)