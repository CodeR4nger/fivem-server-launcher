# 04: Localhost row enrichment regression pins

**What to build:** A saved `localhost`/`127.0.0.1` row behaves like any other saved row when the
local server is publicly listed: the save-time background capture still resolves its cfx id via
the catalog (through the loopback DNS resolution path), and the row enriches with presence
(players/max, game) and icon. Pinned by tests only — the capture and enrichment flows themselves
are not redesigned.

**Blocked by:** 01 (bare localhost is a valid port-less direct address).

**Status:** done

- [x] Saving `localhost:30120` captures the cfx id when the catalog lists the loopback
      `ip:port` endpoint
- [x] A listed localhost row applies presence (online, players/max, game) and icon like any row
      (generic presence/icon paths, pinned by existing suites)
- [x] An unlisted localhost row stays plain/unresolved and never breaks the save or refresh flows
