# 02: ResolveAsync returns a ServerProfile with identity and requirements

**What to build:** the tracer bullet of the ServerProfile phase. Given an address (CFX id, or `cfx.re/join/<id>` with or without scheme), resolving the server now returns a `ServerProfile` grouping the identity (`CfxId`, `ProjectName`) and the `Requirements` derived from what CFX publishes. `ResolvedServer` is replaced by `ServerProfile`. The resolver internally composes the CFX query and the requirements derivation. Current validation is preserved: empty address → exception; server not found → exception.

**Blocked by:** 01 (Extend ServerRequirements with Pure Mode and Steam ticket)

**Status:** resolved

- [ ] Resolving a valid CFX id returns a `ServerProfile` with that `CfxId`
- [ ] The `ServerProfile` includes the `ProjectName` published by the server
- [ ] The `ServerProfile` includes the derived `Requirements` (GameBuild, PureMode, RequestSteamTicket) as CFX publishes them
- [ ] `cfx.re/join/<id>` with and without scheme keep resolving to the correct id
- [ ] Empty address and server-not-found still throw the current exception (no regression)
- [ ] `LauncherSettings` is not modified; the logic lives in the domain
