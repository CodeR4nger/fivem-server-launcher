# 05: Estrchar el catch de ServerCatalog a fallos de red

**What to build:** `ServerCatalog.GetServersAsync` captura `catch (Exception)` completo: correcto para "outage → sin match" (requisito de la fase), pero también enmascara errores de programación (bugs del handler, JSON corrupto de otro origen, etc.). Estrchar a las excepciones de red esperadas: `HttpRequestException` y `TaskCanceledException`. Cualquier otra excepción se propaga al llamador (degradación NO aplica: es un bug, debe ser visible).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `GetServersAsync` captura solo `HttpRequestException` y `TaskCanceledException` → devuelve sin match (comportamiento de outage intacto).
- [x] Una excepción inesperada (p.ej. `InvalidOperationException` en el handler) se propaga, no se traga.
- [x] Test nuevo en `ServerCatalogTests`: handler que lanza una excepción no-red → `LookupByEndPointAsync`/`LookupByIpPortAsync` lanza (no devuelve null).
- [x] Test existente de outage (`throwOnSend: true` → HttpRequestException) sigue verde.
- [x] Suite completa verde.

## Comments