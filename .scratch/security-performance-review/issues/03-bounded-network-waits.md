# 03: Bounded network waits and payloads

**What to build:** The shared `HttpClient` uses the 100 s default timeout and DNS resolution has no
bound, so an unresponsive endpoint keeps the UI stuck in IsBusy for minutes on the connect path.
Downloads (catalog, icons) read bodies without size caps. Bound every interactive wait and cap
response sizes at the seams.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Shared HttpClient gets a short timeout set at the composition root; connect never stalls
      beyond it
- [ ] DNS resolution on the connect path is bounded via a cancellation token on the resolver seam
- [ ] Catalog and icon downloads enforce a maximum payload size; oversized responses degrade like
      outages (no throw)
