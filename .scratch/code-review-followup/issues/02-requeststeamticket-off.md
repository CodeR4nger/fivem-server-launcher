# 02: requestSteamTicket "off" is not treated as absent

**What to build:** the Steam ticket requirement distinguishes the three states CFX allows: published `on` → `true`, published `off` → `false`, not published → `null`. Today an explicit `"off"` is confused with absence (indeterminate), which could mislead a future consumer of `Requirements`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `CfxService` maps `requestSteamTicket: "off"` → `RequestSteamTicket = false`
- [x] `requestSteamTicket: "on"` → `true`
- [x] absent var → `null`
- [x] Tests cover all three cases in `CfxServiceTests`
