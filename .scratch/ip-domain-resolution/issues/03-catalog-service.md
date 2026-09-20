# 03: Servicio Catálogo CFX (streamRedir + búsqueda + cache TTL)

**What to build:** un servicio tipo `CfxService` (seam, sin persistir estado de la UI) que descarga el catálogo completo desde `https://frontend.cfx-services.net/api/servers/streamRedir/`, lo decodifica con la fundación protobuf de la ticket 02, y responde a preguntas como "¿aparece este `ip:port`?" o "¿existe este cfx id?" devolviendo la entrada `master.Server` que matchea. Mantiene el stream en memoria con un TTL (~5 min) para no re-descargar los MB por cada consulta; si el catálogo no está disponible, devuelve "sin match" (degradación manejada por el llamador).

**Blocked by:** 02

**Status:** resolved

- [x] Fetch del stream vía `HttpClient` inyectable (testeable con `FakeHttpMessageHandler`), con cabeceras/User-Agent razonables.
- [x] Decodificación de todos los frames usando el helper protobuf de la 02.
- [x] `LookupByIpPort(ip:port)` matchea contra `connectEndPoints`; `LookupByEndPoint(id)` matchea contra `EndPoint`; sin match → null.
- [x] El resultado queda expuesto para derivar `gamename`, `sv_projectName` y las vars de requisitos (`sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`).
- [x] Cache TTL en memoria (~5 min): una segunda consulta dentro del TTL no re-descarga.
- [x] Con catálogo inalcanzable/fallido → consulta devuelve sin match (no lanza al llamador).
- [x] Tests con `FakeHttpMessageHandler` y fixtures binarios construidos en el test (helper de frames de la 02); estilo Given/When/Then.
- [x] Suite completa sigue verde.

## Comments

- TDD RED→GREEN→REFACTOR completado con 9 tests nuevos en `tests/.../Service/ServerCatalogTests.cs`. Suite completa 90 verdes (81 previos + 9 nuevos).
- `Service/ServerCatalog` (seam, `HttpClient` inyectable + `TimeProvider` + `cacheTtl` opcionales): `LookupByIpPortAsync` (matchea `connectEndPoints`) y `LookupByEndPointAsync` (matchea `EndPoint`); cache TTL 5 min con `TimeProvider` inyectable; fallo de red/status/corrupción → devuelve sin match, no lanza.
- User-Agent `FiveMServerLauncher/0.1` por request (la primera versión con assert `NotNull` siempre pasaba: `HttpHeaderValueCollection` nunca es null → se corrigió el assert a `NotEmpty`, RED real).
- Test helpers nuevos compartidos: `TestProtobufFrames` (frames binarios, DRY con la 02) y `FakeTimeProvider` (avance manual del reloj para TTL).
- REFACTOR: `FakeHttpMessageHandler` ganó soporte `byte[]` + `RequestCount` + modo "lanza HttpRequestException" (fallo de red simulado).