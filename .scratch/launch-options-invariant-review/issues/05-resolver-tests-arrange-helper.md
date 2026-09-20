# 05: Helper de arrange en ServerResolverTests (DRY)

**What to build:** En `tests/FiveMServerLauncher.Tests/Domain/ServerResolverTests.cs`, la repetición `new ServerResolver(cfxService, new ServerRequirementsResolver())` (10×) se centraliza en un helper privado de arrange (método o fábrica local) que toma el `cfxService` y devuelve el resolver. Sin cambio en la lógica probada ni en las aserciones.

**Blocked by:** None

**Status:** resolved

- [x] La creación de `new ServerResolver(...)` no se repite inline en los métodos de test; usa el helper
- [x] Los 9+ tests existentes razonan igual (mismos Given/When/Then y aserciones)
- [x] Suite completa verde (62)

## Comments
- Helper privado `CreateResolver(CfxService)` centraliza `new ServerResolver(cfxService, new ServerRequirementsResolver())`; se reemplazaron las 10 ocurrencias inline.
