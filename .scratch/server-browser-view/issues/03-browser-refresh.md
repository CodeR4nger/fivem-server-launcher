# 03: Manual refresh in the browser

**What to build:** a refresh action in the browser that forces a fresh catalog fetch through
the shared phase-9/10 cooldown (`RefreshAsync(forceRefresh: true)` / `GetSnapshotAsync(true)`
at the enrichment/catalog seam) and reloads the rows; the command is disabled while running
and for the cooldown window, mirroring the saved-servers refresh button. Outages keep the
last loaded rows.

**Blocked by:** 02 (ServerBrowserViewModel with filters).

**Status:** resolved

## Answer

`ServerBrowserViewModel.RefreshCommand` (AsyncRelayCommand) forces the refresh through the
shared `IServerEnrichmentService.RefreshAsync(forceRefresh: true)` cooldown, reloads rows from
the (now warm) catalog, stays disabled through the refresh + injectable cooldown window
(mirroring the saved-servers button), and keeps rows on outage. 4 tests green.

- [ ] Refresh forces a real fetch (TTL bypass) through the shared cooldown seam
- [ ] Command disabled while running + cooldown window
- [ ] Outage keeps the currently displayed rows, no throw
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/server-browser-view/spec.md`
