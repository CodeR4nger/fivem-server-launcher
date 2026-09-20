# 04: Quitar código muerto y aclarar contrato del catálogo

**What to build:** dos hallazgos del eje Standards:
1. `ServerAddress.IsCfxJoinUrl` (método privado) no tiene callers — eliminarlo. Solo queda `IsCfxJoinUrlWithValidId`, que sí se usa.
2. `ServerCatalog.LookupByEndPointAsync` solo lo usan los tests; el resolver usa `LookupByIpPortAsync`. DECISIÓN de la spec: se conserva como parte del contrato del seam del catálogo (buscar un server por su cfx id), pero se documenta el porqué en AGENTS.md para que no parezca dead weight (p.ej. futura UI por id).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `IsCfxJoinUrl` eliminado (sin callers en producción ni tests).
- [x] `LookupByEndPointAsync` se conserva y su razón se documenta en AGENTS.md (contrato del catálogo; lista para consulta por cfx id).
- [x] Suite completa verde (ningún test depende del método borrado).

## Comments