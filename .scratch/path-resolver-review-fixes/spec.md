Status: resolved
Type: spec

# Fixes de post-review de la fase path-resolver

## Problem Statement
Review de `f00d3a7...HEAD`:
- **Spec**: `GetExecutablePathAsync` devolvía null solo de forma implícita ("Should return null only when installed"); el real no respetaba la especificación (devolvía un path inexistente por dentro). Además existía un RedM mapping no solicitado (creepee) y testet de fake tautológicas.
- **Standards**: test ni modo nombres "ViaRealImpl"; inside tests folded `LastQueriedClient` capture (dead); typo `Enhaced`.

## Solution
- `ClientInstallLocator` con ctor inyectable `Func<string,bool> exists` (default: `File.Exists`) — seam inyectable del repo, no seam diferente.
- `GetExecutablePathAsync` devuelve null cuando no está instalado; `GetPath` no mapea RedM (creepee removida).
- Tests actualizados: `ClientInstallLocator` con fake exists → real impl testable; renombrado consistente "null al no instal" sin sufijos "ViaRealImpl"; removido dead `LastQueriedClient` del fake.

## Testing Decisions
- Suite verde (117 = antes 114 + 3 RED/GREEN nuevos).
- Mantener `AGENTS.md` updated (suite count).

## Out of Scope
- Steam/Discord detections (otra fase), PathResolver enrichments (custom/etc.), YAGNI abstractions.