# 07: Forced-refresh seam + shared cooldown

**What to build:** an explicit on-demand enrichment refresh that bypasses the catalog TTL and
is rate-limited by a shared cooldown:

- `ServerCatalog` gains a force path (bypass/invalidate TTL for the next fetch).
- `IServerEnrichmentService` gains a forced variant (`RefreshAsync(force: true)` or
  equivalent) that passes the force through to the catalog.
- Cooldown: forced fetches are refused when one happened within a short window (~15 s,
  injectable for tests); a refused force serves the cached snapshot, never throws, never
  blocks.
- Icons keep their (CfxId, iconVersion) cache semantics; non-forced calls and the per-minute
  loop behave exactly as before.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

Seam: `ServerCatalog.GetSnapshotAsync(bool forceRefresh)` (force bypasses TTL; on outage the
force path serves the warm cache, non-forced keeps returning null). `IServerEnrichmentService
.RefreshAsync(bool forceRefresh = false)` applies a shared injectable cooldown
(`DefaultForceCooldown` 15 s; real time via `TimeProvider`): refused forces fall back to the
normal TTL path. `FakeServerEnrichmentService` tracks `ForcedRefreshCalls`. Suite 470 green.

- [ ] Forced refresh performs a real HTTP fetch even inside the TTL window
- [ ] A second forced refresh inside the cooldown window serves cache (request count
      unchanged)
- [ ] After the cooldown window a forced refresh refetches
- [ ] Non-forced refresh keeps TTL semantics unchanged
- [ ] Cooldown never throws and never blocks the caller
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9d; shared by phases
10/12)
