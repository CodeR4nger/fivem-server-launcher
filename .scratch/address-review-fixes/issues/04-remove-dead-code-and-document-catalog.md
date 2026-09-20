# 04: Remove dead code and clarify the catalog contract

**What to build:** two findings from the Standards axis:
1. `ServerAddress.IsCfxJoinUrl` (private method) has no callers — remove it. Only `IsCfxJoinUrlWithValidId` remains, which is used.
2. `ServerCatalog.LookupByEndPointAsync` is used only by tests; the resolver uses `LookupByIpPortAsync`. Spec DECISION: it is kept as part of the catalog seam contract (look up a server by its cfx id), but the rationale is documented in AGENTS.md so it doesn't look like dead weight (e.g. future id-based UI).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `IsCfxJoinUrl` removed (no callers in production or tests).
- [x] `LookupByEndPointAsync` is kept and its rationale documented in AGENTS.md (catalog contract; ready for cfx-id lookup).
- [x] Full suite green (no test depends on the deleted method).

## Comments
