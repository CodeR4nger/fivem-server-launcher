# 02: Extraer la forma de dirección de servidor a un solo lugar

**What to build:** el concepto "dirección de servidor" (`cfx.re/join/<id>`) tiene hoy un único dueño en el dominio, en lugar del mismo literal duplicado en tres sitios (`ExtractCfxId` de `ServerResolver` y la validación/construcción de `FiveMLaunchOptions`). Un helper/tipo compartido (DRY) provee: validar si una address tiene forma de servidor, derivar la address desde un `CfxId`, y extraer el `CfxId` desde una address. Sin cambio de comportamiento observable.

**Blocked by:** 01 (Endurecer y blindar la validación de la fábrica)

**Status:** resolved

- [x] El literal `cfx.re/join/` existe una sola vez en el código de dominio
- [x] `ServerResolver.ExtractCfxId` usa la pieza compartida y sigue extrayendo el id igual que hoy (no regresión)
- [x] La validación/construcción de `FiveMLaunchOptions` usa la pieza compartida
- [x] La suite completa sigue verde tras el refactor (sin tests nuevos de comportamiento)