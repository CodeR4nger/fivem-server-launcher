# 01: Extend ServerRequirements with Pure Mode and Steam ticket

**What to build:** the domain building block recognizes the three requirements CFX publishes. Today `ServerRequirements` only exposes `GameBuild`; it is extended to also expose Pure Mode (when the server publishes `sv_pureLevel`) and the Steam ticket requirement (when it publishes `requestSteamTicket`). Each requirement stays absent (`null`) when the server doesn't publish it; the launcher doesn't assume arbitrary values.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] Given a `CfxServerInfo` with `PureLevel`, the result exposes a `PureMode` with that value
- [ ] Given a `CfxServerInfo` with `RequestSteamTicket`, the result exposes a `RequestSteamTicket` with that value
- [ ] Given a `CfxServerInfo` without those vars, the corresponding fields remain `null`
- [ ] The existing `GameBuild` keeps deriving from `sv_enforceGameBuild` (no regression)
- [ ] Mapping driven by `CfxServerInfo` (single source), without touching `Service` or `LauncherSettings`
