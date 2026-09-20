# 02: Connect — ServerProfile → options → fivem:// URI

**What to build:** the "connect to a server" intent. Given a resolved `ServerProfile`, a `FiveMLaunchOptions` is built whose address derives from the profile's `CfxId`, whose `GameBuild`/`PureMode` come from its `Requirements` (CFX-published: server requirements win over any manual ones) and whose `GameClient` is the profile's. `ToUri()` produces `fivem://connect/<addr>` with `?-b<build>` and `?-pure_<level>` only for present requirements; it returns `null` when there is no address or when `GameClient` is `FiveMEnhanced` (that client doesn't support direct connection, verified).

**Blocked by:** 01 (Base FiveMLaunchOptions model with validated factory)

**Status:** resolved

- [x] Given a valid `ServerProfile`, `ToUri()` returns `fivem://connect/cfx.re/join/<CfxId>`
- [x] With published `GameBuild`, the URI includes `?-b<build>`
- [x] With published `PureMode`, the URI includes `?-pure_<level>`
- [x] With absent requirements, the URI doesn't include those args (assumes no values)
- [x] With both requirements, order is stable (build before pure)
- [x] With `GameClient = FiveMEnhanced`, `ToUri()` returns `null`
- [x] Without an address (open-directly intent), `ToUri()` returns `null`
- [x] Requirements come from the profile's `ServerRequirements`, with no manual override
