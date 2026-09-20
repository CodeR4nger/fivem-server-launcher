# 08: Decidir Middle Men: ConfigurationRepository y ServerRequirementsResolver

**What to build:** decisión de diseño sobre dos clases que mayormente delegan: `ConfigurationRepository` (Save/Load con null-guards) y `ServerRequirementsResolver` (mapper de 3 líneas). Dos caminos: justificarlas como seams intencionales (documentándolo en DOC.md y un ADR) o inlinearlas donde se consumen. Es un ticket de documentación/refactor según lo que se decida.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Se registra la decisión (ADR en `docs/adr/` o nota en DOC.md)
- [ ] Si se mantienen: DOC.md explica por qué son seams (testabilidad/separación) y su frontera
- [ ] Si se inlinean: el consumidor llama al target directo y se borran las clases intermedias
- [ ] Suite verde tras el cambio