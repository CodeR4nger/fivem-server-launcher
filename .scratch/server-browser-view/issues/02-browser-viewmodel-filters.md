# 02: ServerBrowserViewModel with filters

**What to build:** a `ServerBrowserViewModel` that owns the browser's state: loads the catalog
snapshot (TTL-respecting) on demand, exposes the filtered view, and holds the filters — game
(All / FiveM / FiveM Enhanced / RedM), hide-full, hide-empty, and live name search
(case-insensitive, substring on display name) — combined as AND. Outage (null snapshot) shows
an honest empty state and never throws. Counts/rows come from the phase-12 item mapping.

**Blocked by:** 01 (Browser item mapping).

**Status:** resolved

## Answer

`ViewModels/ServerBrowserViewModel` (INPC, `ServerCatalog` seam): `LoadAsync` maps snapshot
(`Data`-less entries skipped), outage → `LoadFailed` + rows kept. Filters `GameFilter`
(null=All), `HideFull`, `HideEmpty`, `SearchText` combine as AND over `ServersView`
(refactored to a single `SetFilter` helper). 7 tests green.

- [ ] Load maps snapshot entries to rows (skipping `Data`-less entries)
- [ ] Game filter restricts to one game; All shows everything
- [ ] Hide-full excludes `players >= max` (max > 0); hide-empty excludes `players == 0`
- [ ] Name search filters live (case-insensitive substring), combines with all other filters
- [ ] Outage → empty state flag set, no exception
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/server-browser-view/spec.md`
