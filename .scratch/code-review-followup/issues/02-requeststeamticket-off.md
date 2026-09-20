# 02: requestSteamTicket "off" no se trata como ausente

**What to build:** el requisito de ticket de Steam distingue los tres estados que CFX permite: publicado `on` → `true`, publicado `off` → `false`, no publicado → `null`. Hoy un `"off"` explícito se confunde con ausencia (indeterminado), lo que podría engañar a un futuro consumidor de `Requirements`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `CfxService` mapea `requestSteamTicket: "off"` → `RequestSteamTicket = false`
- [x] `requestSteamTicket: "on"` → `true`
- [x] var ausente → `null`
- [x] Tests cubren los tres casos en `CfxServiceTests`