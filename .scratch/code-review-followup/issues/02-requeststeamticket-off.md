# 02: requestSteamTicket "off" no se trata como ausente

**What to build:** el requisito de ticket de Steam distingue los tres estados que CFX permite: publicado `on` → `true`, publicado `off` → `false`, no publicado → `null`. Hoy un `"off"` explícito se confunde con ausencia (indeterminado), lo que podría engañar a un futuro consumidor de `Requirements`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `CfxService` mapea `requestSteamTicket: "off"` → `RequestSteamTicket = false`
- [ ] `requestSteamTicket: "on"` → `true`
- [ ] var ausente → `null`
- [ ] Tests cubren los tres casos en `CfxServiceTests`