# 03: Consolidate gamename access in CfxVars + reconcile EndPoint doc

**What to build:** `CfxVars.TryGetInt` null-guards the dict but `MapGameClient` does not, forcing a different ad-hoc null-check in each caller (`CfxService` vs `ServerResolver`). Consolidate into a single `CfxVars` member (e.g. `TryGetGameClient(dict) → GameClient?` with the null-guard inside) that unifies both call sites' shapes. Also, reconcile AGENTS.md: distinguish that the `EndPoint` of the `/single/` response is a connection endpoint (not used as id) while the `EndPoint` of a streamRedir catalog frame is the canonical cfx id (which is why `ServerResolver` can assign it to `CfxId`).

**Blocked by:** 01 (resolver is touched in 01; avoid conflict)

**Status:** resolved

- [x] `CfxVars` exposes a `gamename` access with null-guard (unified shape) used by `CfxService` and `ServerResolver`.
- [x] `CfxService.cs` no longer does the ad-hoc null-check of `Vars` for `gamename`.
- [x] `ServerResolver.BuildValidatedProfile` uses the same access as `CfxService`.
- [x] AGENTS.md "Current state" distinguishes `EndPoint` of `/single/` (connection endpoint, not id) vs `EndPoint` of the catalog (canonical cfx id). Without changing `CfxId = server.EndPoint` in the resolver.
- [x] Full suite green (`CfxServiceTests` and resolver tests keep passing unmodified).

## Comments
