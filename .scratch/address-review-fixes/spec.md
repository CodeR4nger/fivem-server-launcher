Status: resolved
Type: spec

# Fixes de post-review de la fase ip-domain-resolution

## Problem Statement

El code review del diff `5dc7b7f...HEAD` (ejes Standards y Spec, skill `code-review`) encontró hallazgos de código, de spec y de documentación tras la fase `ip-domain-resolution`. No hay bugs funcionales críticos (todas las casillas de los 5 tickets originales se cumplen), pero hay: (a) una discrepancia spec↔código en validación de dominios (spec.md:44 "1+ labels" vs implementación `labels >= 2` + TLD-con-letra), (b) `InvalidAddressException` recibe el `cfxId` extraído en vez de la dirección original (spec.md:30 no fija el contrato), (c) duplicación en la split host:port y en el acceso a `gamename`, (d) código muerto (`IsCfxJoinUrl`), (e) `catch (Exception)` que enmascara errores de programación, (f) contradicción documental: AGENTS.md dice que `EndPoint` es "connection endpoint, no el id" pero `ServerResolver.BuildValidatedProfile` usa `EndPoint` del frame del catálogo como `CfxId`.

Esta spec resuelve primero las decisiones de alcance (validación de dominios, contrato de excepción, semántica de `EndPoint` en catálogo) y luego los tickets de código que las implementan.

## Solution

- **Validación de dominios (decidido):** se aceptan **1+ labels** (alinea con spec.md:44). Los casos de 1 label (`localhost:30120`) se clasifican como `DomainPort` si el label es válido. La regla "TLD con letra" aplica solo cuando hay 2+ labels (evita que `999.56.120.52:30320`, una IP con octeto inválido, pase como dominio numérico). Esta regla queda **escrita en spec.md** (hoy vive solo en un comentario de ticket).
- **Contrato de excepción (decidido):** `InvalidAddressException` se construye siempre con la **dirección original** que ingresó el usuario (no con el id extraído). Todos los call sites del resolver pasan la dirección de entrada sin transformar.
- **Semántica de `EndPoint` en catálogo (decidido):** en los frames de `streamRedir`, `EndPoint` es el **id canónico del servidor** (cfx id); AGENTS.md describe el `EndPoint` de la **respuesta `/single/`** (que sí es un connection endpoint y NO se usa como id). Se reconcilia la documentación de AGENTS.md para distinguir ambos, sin cambiar `CfxId = server.EndPoint` en el resolver.
- **Otros hallazgos de código** pasan a tickets de refactor/fix listados abajo, todos con TDD (RED→GREEN→REFACTOR) y sin cambiar comportamiento externo salvo donde lo pide una decisión.

## User Stories

1. Como jugador, quiero poder entrar a `localhost:30120` cuando levanto un server local, para no depender de un dominio de 2+ labels (relajación de validación).
2. Como jugador, quiero que el error de una dirección inválida muestre o conserve la dirección tal como la escribí, para diagnosticar el fallo rápido.
3. Como desarrollador, quiero que el código del resolver/catálogo no duplique la split `host:port` ni el acceso a `gamename`, para mantener DRY.
4. Como desarrollador, quiero que los errores de red inesperados (no de CFX) no se silencien, para no ocultar bugs.

## Implementation Decisions

- `ServerAddress.IsValidDomain` se relaja a 1+ labels: `labels.Length >= 1`, cada label válida, y si `labels.Length >= 2` el TLD debe contener una letra. Tests nuevos en `ServerAddressTests` para `localhost:30120` (DomainPort) y para IP inválida de 4 octetos tipo `999.56.120.52:30320` (sigue Unknown → excepción).
- `ServerResolver`: `InvalidAddressException` con la dirección original en los 3 caminos que hoy la lanzan (whitespace, forma unknowna, CFX null). Test verifica que el mensaje/excepción usa la entrada original.
- **DOCUMENTACIÓN:** `AGENTS.md` "Current state" distingue: `EndPoint` en respuesta `/single/` = connection endpoint (no se usa como id); `EndPoint` en frames de catálogo = cfx id canónico. Se actualiza la línea del resolver y la de `CfxService` según corresponda.
- Refactor DRY 1 (ticket 02): método compartido `SplitHostPort` en `ServerAddress` o helper, usado por `IsIpPort`/`IsDomainPort` y por `ServerResolver` (reemplaza `GetHost`/`GetPort`).
- Refactor DRY 2 (ticket 03): `CfxVars.TryGetGameClient(IDictionary<string,string>? vars)` — devuelve `(bool found, GameClient? gameClient)` o patrón equivalente — usado por `CfxService` y `ServerResolver`, centralizando la clave `"gamename"` y el null-guard.
- Dead code (ticket 04): eliminar `ServerAddress.IsCfxJoinUrl` (sin callers). `LookupByEndPointAsync` se **conserva** (documentado en AGENTS.md como parte del seam del catálogo; usado por tests); se deja claro por qué se expone (parte del contrato del catálogo, lista para futura UI/búsqueda por id).
- Catch fino (ticket 05): `ServerCatalog.GetServersAsync` captura solo `HttpRequestException` y `TaskCanceledException` (outage/red); cualquier otra excepción se propaga. No cambia la API pública (sigue devolviendo sin-match en outage).
- Sin cambios de UI, sin Cambiar contratos públicos existentes (firmas de `ServerAddress`, `ServerProfile`, `ServerCatalog` se mantienen salvo refactors internos).
- Los tickets 02-05 no cambian comportamiento observable salvo el 05 (propagar errores de programación); el 01 sí cambia clasificación (localhost) y el contrato de excepción (dirección original). Tests correspondientes en cada ticket.

## Testing Decisions

- TDD por slice: cada ticket en `RED → GREEN → REFACTOR` con suite completa verde al final.
- Tests para el ticket 01 en `ServerAddressTests` (clasificación localhost + IP inválida persiste como Unknown) y `ServerResolverTests` (excepción con dirección original).
- Tests para ticket 02: refactor de extracción, la suite existente es la red de seguridad (sin tests nuevos salvo que el refactor lo amerite).
- Ticket 03: tests de `CfxServiceTests` y los del resolver siguen verdes tras la consolidación; opcional un test directo de `CfxVars.TryGetGameClient`.
- Ticket 04: eliminar test que no exista; la suite completa cubre la no-regresión.
- Ticket 05: `ServerCatalogTests` con `FakeHttpMessageHandler(throwOnSend: true)` (HttpRequestException → sin match) y un nuevo caso: un handler que lanza `InvalidOperationException` → la excepción se propaga (no se traga).

## Out of Scope

- Cualquier funcionamiento nuevo (UI, advertencia de servidor no validado, favicons/players del catálogo).
- Reabrir la fase `ip-domain-resolution` (spec y tickets ya resueltos se mantienen como están).
- Cambiar `ServerProfile.Address`/`IsCfxValidated` o el contrato del catálogo.
- Refactor ruidoso de `ServerAddress` más allá de la split host:port y la dead code.

## Further Notes

- Doc del review (hallazgos): `.scratch/...` no; el review vive en la conversación. Referencias de código: `src/FiveMServerLauncher/Domain/ServerAddress.cs` (`IsCfxJoinUrl`, `IsIpPort`, `IsDomainPort`), `Domain/ServerResolver.cs` (`GetHost`, `GetPort`, `BuildValidatedProfile`), `Service/CfxVars.cs`, `Service/CfxService.cs`, `Service/ServerCatalog.cs` (`GetServersAsync` catch), `src/FiveMServerLauncher/AGENTS.md`.
- Original spec de la fase: `.scratch/ip-domain-resolution/spec.md`; tickets 01-05 resueltos.
- Tras cerrar: actualizar AGENTS.md (Statement actual: nota de EndPoint, si aplica) y commitear con conventional commits en inglés.