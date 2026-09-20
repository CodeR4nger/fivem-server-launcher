# 04: Resolver IP/dominio en ServerResolver con degradación a no validado

**What to build:** `ServerResolver.ResolveAsync` acepta las 4 formas del clasificador (ticket 01). Para `CfxId`/`CfxJoinUrl` mantiene el flujo actual. Para `IpPort`/`DomainPort` usa el servicio de catálogo (ticket 03): si el endpoint aparece publicado, devuelve un `ServerProfile` **validado** con los datos del catálogo (incl. requisitos y gamename); si no aparece (o el DNS del dominio falla), devuelve un **perfil conectable no validado** con la dirección tal cual, sin inventar requisitos, marcado con el nuevo `IsCfxValidated = false`. Las formas inválidas (clasificador) lanzan `InvalidAddressException`. El DNS del dominio se resuelve a IP para el matcheo contra `connectEndPoints` vía un seam inyectable (real + fake en tests).

**Blocked by:** 01, 03

**Status:** resolved

- [x] `ServerProfile` gana `IsCfxValidated` (true para perfiles resueltos desde CFX/catálogo; false para conectable directo degradado).
- [x] `CfxId`/`CfxJoinUrl` → flujo actual intacto (buscar en CFX por id; null → `InvalidAddressException`).
- [x] `IpPort` encontrado en catálogo → perfil validado con datos del catálogo (requirements + gamename cuando el catálogo los publica).
- [x] `IpPort` no publicado → perfil conectable `IsCfxValidated=false`, `CfxId`/`ProjectName`/`GameClient`/`Requirements` sin datos inventados.
- [x] `DomainPort` → DNS a IP (seam), matcheo por IP:port en catálogo; DNS fallido → perfil conectable no validado (no excepción).
- [x] Formas inválidas/desconocidas del clasificador → `InvalidAddressException`.
- [x] Sin catálogo disponible (fallo de red) en un IP/dominio → perfil conectable no validado (degradación, no excepción).
- [x] Tests en `ServerResolverTests` con fakes (HTTP + DNS), estilo Given/When/Then; `CreateResolver` actualizado; suite completa verde.

## Comments

- TDD RED→GREEN→REFACTOR completado con 7 tests nuevos en `tests/.../Domain/ServerResolverTests.cs` (los 10 previos siguen verdes). Suite completa 97 verdes (90 previos + 7 nuevos).
- `ServerResolver` ahora inyecta `ServerCatalog` + `IDnsResolver` (seam DNS nuevo en `Domain`, fake `FakeDnsResolver` en tests). `ResolveAsync` ramifica por `ServerAddressKind`:
  - `CfxId`/`CfxJoinUrl` → flujo CFX existente + `IsCfxValidated=true`.
  - `IpPort` → `LookupByIpPortAsync`; encontrado → validado; no → conectable no validado con la dirección cruda.
  - `DomainPort` → DNS host→IP; `LookupByIpPortAsync` por `ip:port` y luego por el host crudo; DNS fallido o sin match → no validado (sin excepción).
  - `Unknown` → `InvalidAddressException`. Whitespace y vacío también.
- Perfil no validado: `Address` = dirección cruda, resto vacío/nulls. Perfil validado desde catálogo: `CfxId`=EndPoint, `Address`=primer connectEndPoints, ProjectName=`sv_projectName`→hostname, GameClient/Requirements desde vars.
- `ServerProfile` ganó `Address` (para el flujo conectar-directo) y `IsCfxValidated`.
- REFACTOR (DRY): mapeo de vars extraído a `Service/CfxVars` compartido por `CfxService` y `ServerResolver` (gamename→GameClient, int, steam ticket); `CfxService` quedó más delgado.
- Fix en el camino: `LookupByIpPortAsync` era null-unsafe con `Data` ausente en entradas del catálogo sin `Data` (NRE en la predicción) → null-safe.