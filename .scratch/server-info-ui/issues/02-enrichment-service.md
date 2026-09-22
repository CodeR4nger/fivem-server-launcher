# 02: ServerEnrichmentService (catalog + icon)

**What to build:** new `Service/ServerEnrichmentService` seam: `RefreshAsync()` maps CfxId →
presence/`Players`/`MaxPlayers` from one streamRedir catalog download (online = present);
`GetIconAsync(cfxId)` tracks `/single/{id}` `iconVersion` and returns cached icon bytes from
`https://frontend.cfx-services.net/api/servers/icon/{id}/{iconVersion}.png`, refetching only on
version change. Injectable `HttpClient` + `TimeProvider` (catalog TTL). Outage/corrupt → no
update, never throws.

**Blocked by:** none.

**Status:** ready-for-agent

- [ ] Catalog frame → presence/players/max map; online = catalog presence.
- [ ] Icon bytes fetched per (CfxId, iconVersion), cached, re-fetched only on version change.
- [ ] Outage/corrupt frames and failed icon fetches degrade silently (never throw).