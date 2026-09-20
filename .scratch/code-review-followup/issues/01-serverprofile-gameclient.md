# 01: ServerProfile conserva GameClient

**What to build:** la composición de `ResolveAsync` no pierde el `GameClient` (FiveM | FiveMEnhanced | RedM) que CFX publica en `gamename`. El `ServerProfile` que devuelve el resolver lo expone para que capas posteriores (p.ej. `Requirements`) puedan consultarlo.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `ServerProfile` expone un campo `GameClient`
- [x] Resolver un servidor con `gamename: gta5enhanced` devuelve el perfil con ese GameClient mapeado
- [x] Un servidor sin `gamename` deja el campo en `null` (no asumir cliente)
- [x] Test cubre el mapping en `ServerResolver` (seam alto)