# 05: Catalog single-flight and enrichment request hygiene

**What to build:** The multi-megabyte catalog is downloaded concurrently by every caller with a
cold/expired cache (connect, per-minute loop, browser refresh) with unsynchronized cache writes,
and the per-minute enrichment loop probes `/single/{id}` once per saved row serially to learn icon
versions even though the snapshot already carries `iconVersion` in the vars. Browser scrolling
fires one unbounded icon GET per realized row, and the icon cache never evicts.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Concurrent snapshot requests share one in-flight download (single-flight) with a coherent
      cache write
- [x] Presence carries the catalog-published iconVersion so the enrichment loop uses the direct
      `GetIconAsync(id, version)` path with no per-minute `/single/` probes
- [x] Concurrent icon requests for the same id are de-duplicated with a bounded in-flight count
- [x] The icon cache is bounded (oldest versions evicted) instead of app-lifetime growth

## Landed decisions (recorded per the spec)

- A forced snapshot refresh joins an in-flight non-forced download instead of starting its own —
  the in-flight download is fresher than any cache could be; on outage the forced path still
  serves the warm cache. Tested (`GetSnapshot_WhenForcedCallOverlapsInFlightFetch_ShouldJoinIt`).
- The catalog snapshot is the single proof of icon-version presence/absence: the `/single/`
  version-probe path (`GetIconAsync(cfxId)` + `GetIconVersionAsync`) was deleted. The enrichment
  loop and the browser both skip rows without a published `iconVersion`, so enrichment traffic
  never touches `/single/` (the browser's per-open repeated probes for version-less rows were the
  hygiene leak). Accepted trade-off: an icon added within the catalog TTL shows up to ~5 min late.
- Icon in-flight tasks are registered *before* the download starts (TCS + driver). Registering
  after let a synchronously completing download park its completed task as in-flight, which then
  served stale bytes with no network call after an LRU eviction — found by review, fixed, pinned by
  `GetIconAsync_WhenDownloadCompletesSynchronously_ShouldNotParkCompletedTaskAsInFlight`.
- Spec story 5 (saved-list refresh fetching icons concurrently) stays waived: with the direct
  version path + LRU cache the per-minute steady state is cache hits (near-zero requests), and
  each first-cycle download is bounded and cached; the serial await in `RefreshServerInfoAsync`
  never blocks the UI thread. Revisit only if first-cycle latency with many saved rows proves
  noticeable.
