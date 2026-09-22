# 03: MainViewModel enrichment + per-minute refresh

**What to build:** `SavedServerItem` gains `CfxId`, `Players`/`MaxPlayers` (`int?`), `Online`
(`bool`), `IconSource`; `MainViewModel` wires save-time CfxId capture (sync for CFX-form, async
background catalog lookup for IP:port/domain, persisted via repository `Update`) and a refresh
loop (injectable cadence, default ~60 s via `TimeProvider`) that applies enrichment to rows with
a CfxId and leaves others plain, running while the window is open.

**Blocked by:** 01, 02.

**Status:** ready-for-agent

- [x] Save-time capture: CFX-form immediate; IP:port/domain background lookup + persist; dialog never blocked.
- [x] SavedServerItem exposes enrichment state with PropertyChanged wiring.
- [x] Refresh loop updates enriched rows; null-CfxId rows stay plain.
- [x] Cadence/loop behind injectable time; loads persisted CfxId at startup.