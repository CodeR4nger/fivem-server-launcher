# 07: Renombrar ExtractCfxIdIfValidUrl y aclarar mapping EndPoint→CfxId

**What to build:** el método `ExtractCfxIdIfValidUrl` de `ServerResolver` no valida nada: solo extrae el substring de `cfx.re/join/`. Se renombra para decir lo que hace (`ExtractCfxId`). Además se aclara el mapping de `CfxService.cs` que asigna `EndPoint` a `CfxId` (los fixtures usan una URL como EndPoint y el resolver la devuelve como id).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] El método se renombra a un nombre que refleja su comportamiento real
- [ ] Los tests existentes siguen verdes
- [ ] Se documenta/aclara por qué `EndPoint` alimenta `CfxId` (o se corrige el mapeo)
- [ ] Sin cambios de comportamiento