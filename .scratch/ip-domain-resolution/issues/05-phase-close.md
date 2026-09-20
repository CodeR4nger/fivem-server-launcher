# 05: Cierre de la fase (AGENTS.md, lanzamiento, commit)

**What to build:** la fase `ip-domain-resolution` queda documentada y lanzada. Actualizar `AGENTS.md` (estado actual: ResolveAsync soporta las 4 formas, `ServerProfile.IsCfxValidated`, servicio de catálogo streamRedir + `Google.Protobuf`, quitar TODOs de IP/domain y de validación, nuevos conteos de tests/deps), verificar el build y toda la suite verde, y commitear con conventional commits en inglés.

**Blocked by:** 04

**Status:** resolved

- [x] `AGENTS.md` refleja el estado real: 4 formas de dirección en `ServerResolver`, `ServerProfile.IsCfxValidated`, catálogo vía `streamRedir` con protobuf + cache TTL, DNS seam, dependencia `Google.Protobuf`/`Grpc.Tools`.
- [x] TODOs abiertos de IP/domain y de validación removidos del código/specs.
- [x] `dotnet build` y toda la suite de tests verdes (con el conteo final actualizado donde se mencione).
- [x] Commit(s) con conventional commits en inglés (p.ej. `feat(domain): resolve ip and domain addresses`, `chore(tickets): ...`).
- [x] Paper trail de la fase cerrado: casillas de los 5 tickets verificadas contra el código.

## Comments

- Todas las casillas de los 5 tickets verificadas contra el código. Suite: **97 tests verdes** (0 errores de build, 0 warnings reportados en el build).
- Commits de la fase (conventional en inglés):
  - `feat(domain): resolve ip and domain addresses with streamRedir catalog`
  - `chore(tickets): close ip-domain-resolution phase`