# 05: Catalog single-flight and enrichment request hygiene

**What to build:** The multi-megabyte catalog is downloaded concurrently by every caller with a
cold/expired cache (connect, per-minute loop, browser refresh) with unsynchronized cache writes,
and the per-minute enrichment loop probes `/single/{id}` once per saved row serially to learn icon
versions even though the snapshot already carries `iconVersion` in the vars. Browser scrolling
fires one unbounded icon GET per realized row, and the icon cache never evicts.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Concurrent snapshot requests share one in-flight download (single-flight) with a coherent
      cache write
- [ ] Presence carries the catalog-published iconVersion so the enrichment loop uses the direct
      `GetIconAsync(id, version)` path with no per-minute `/single/` probes
- [ ] Concurrent icon requests for the same id are de-duplicated with a bounded in-flight count
- [ ] The icon cache is bounded (oldest versions evicted) instead of app-lifetime growth
