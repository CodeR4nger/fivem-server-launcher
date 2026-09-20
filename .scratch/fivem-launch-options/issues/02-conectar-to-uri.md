# 02: Conectar — ServerProfile → opciones → URI fivem://

**What to build:** la intención "conectar a un servidor". Dado un `ServerProfile` resuelto, se construye un `FiveMLaunchOptions` cuya address deriva del `CfxId` del perfil, cuyo `GameBuild`/`PureMode` vienen de sus `Requirements` (CFX-published: los requisitos del servidor ganan sobre cualquier manual) y cuyo `GameClient` es el del perfil. `ToUri()` produce `fivem://connect/<addr>` con `?-b<build>` y `?-pure_<nivel>` solo para requisitos presentes; devuelve `null` cuando no hay address o cuando `GameClient` es `FiveMEnhanced` (ese cliente no soporta conexión directa, verificado).

**Blocked by:** 01 (Modelo base FiveMLaunchOptions con fábrica validada)

**Status:** resolved

- [x] Dado un `ServerProfile` válido, `ToUri()` devuelve `fivem://connect/cfx.re/join/<CfxId>`
- [x] Con `GameBuild` publicado, la URI incluye `?-b<build>`
- [x] Con `PureMode` publicado, la URI incluye `?-pure_<nivel>`
- [x] Con requisitos ausentes, la URI no incluye esos args (no asume valores)
- [x] Con ambos requisitos, el orden es estable (build antes de pure)
- [x] Con `GameClient = FiveMEnhanced`, `ToUri()` devuelve `null`
- [x] Sin address (intención abrir directo), `ToUri()` devuelve `null`
- [x] Los requisitos provienen de `ServerRequirements` del perfil, sin override manual