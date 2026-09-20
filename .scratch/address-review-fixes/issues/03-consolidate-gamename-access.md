# 03: Consolidar acceso a gamename en CfxVars + reconciliar doc de EndPoint

**What to build:** `CfxVars.TryGetInt` null-guarda el dict pero `MapGameClient` no, forzando null-check ad-hoc distinto en cada caller (`CfxService` vs `ServerResolver`). Consolidar en un solo miembro de `CfxVars` (p.ej. `TryGetGameClient(dict) → GameClient?` con el null-guard adentro) que iguale las formas de ambos call sites. Además, reconciliar AGENTS.md: distinguir que el `EndPoint` de la respuesta `/single/` es un connection endpoint (no se usa como id) mientras que el `EndPoint` de un frame de catálogo streamRedir es el cfx id canónico (por eso `ServerResolver` puede asignarlo a `CfxId`).

**Blocked by:** 01 (resolver se toca en 01; evitar conflicto)

**Status:** resolved

- [x] `CfxVars` expone un acceso a `gamename` con null-guard (forma unificada) usado por `CfxService` y `ServerResolver`.
- [x] `CfxService.cs` deja de hacer el null-check ad-hoc de `Vars` para `gamename`.
- [x] `ServerResolver.BuildValidatedProfile` usa el mismo acceso que `CfxService`.
- [x] AGENTS.md "Current state" distingue `EndPoint` de `/single/` (connection endpoint, no id) vs `EndPoint` del catálogo (cfx id canónico). Sin cambiar `CfxId = server.EndPoint` en el resolver.
- [x] Suite completa verde (`CfxServiceTests` y resolver tests siguen pasando sin modificaciones).

## Comments