# 05: Resolver el conflicto de spec sobre PreferredClient

**What to build:** la spec de `FiveMLaunchOptions` se contradice a sí misma: dice que "al abrir directo, de `LauncherSettings.PreferredClient`" pero también que "no se toca `LauncherSettings`". Se documenta que el `GameClient` al abrir directamente lo decide el llamador (p.ej. la UI, leyendo `LauncherSettings` a su nivel) y que esta fase no presenta ninguna fábrica que lea `LauncherSettings`. Doc-only: se ajusta la spec y, si corresponde, DOC.md, sin cambios de código.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] La spec de `.scratch/fivem-launch-options/spec.md` ya no se contradice sobre `PreferredClient` (queda claro quién decide el `GameClient` al abrir directo)
- [x] No se introduce código que lea `LauncherSettings`
- [x] La suite completa sigue verde