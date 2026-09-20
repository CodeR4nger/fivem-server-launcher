# 02: MainViewModel muestra error con dirección inválida y abre cliente para Enhanced

**What to build:** dos comportamientos:
1. `ConnectAsync` con una dirección inválida (p.ej. whitespace, unknown) captura `InvalidAddressException` y muestra `StatusText="Dirección inválida"` sin lanzar nada; `IsBusy` vuelve a false.
2. `ConnectAsync` cuando `GameLauncher` devuelve `OpenClient(GameClient)` (servidor Enhanced) muestra `StatusText="Abriendo ..."` sin lanzar proceso.

**Blocked by:** 01

**Status:** resolved

- [x] Test: dirección inválida → `StatusText="Dirección inválida"`, `IsBusy=false`, fake proceso sin calls.
- [x] Test: `OpenClient` Enhanced → `StatusText="Abriendo FiveMEnhanced..."`, fake proceso sin calls.
- [x] `IsBusy=false` incluso si `ConnectAsync` lanza (`finally`).
- [x] Suite completa verde (109).

## Comments