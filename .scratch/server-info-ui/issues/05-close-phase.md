# 05: Close phase + manual E2E

**What to build:** full suite green, docs reconciled (AGENTS.md, roadmap phase status), code
review clean, commit. Manual E2E: icon + players/max + online on CFX-form rows, IP:port capture
in the background, no enrichment for unresolvable ids, error-free when CFX is down.

**Blocked by:** 04.

**Status:** resolved

- [x] Full suite green (357 tests, build clean, 0 errors besides pre-existing CS8619).
- [x] Docs reconciled (AGENTS.md; roadmap status; spec status `done`; ticket checkboxes).
- [x] Code review (Standards + Spec): findings addressed — shared `ServerCatalog` instance in
      composition root; `RefreshAsync` never throws on duplicate EndPoints / corrupt `/single/`
      payload; enrichment loop survives a throwing refresh; removed unused `AddressKind` and
      `Enrichment` test hook; added repository `Update` CfxId-persistence test.
- [x] Manual E2E verified by the user (all rows, tags, colors, OFFLINE/UNRESOLVED working).
- [x] Commit (`0ebc5f2`).

## Comments

- 2026-09-28: ticket statuses in 01–05 were stale (`ready-for-agent`/`in-progress` although the
  phase shipped in `0ebc5f2` on 2026-09-22). Re-verified each ticket's claims against the code
  (`SavedServer.CfxId` + `IsValidCfxId` gate; `ServerEnrichmentService` presence map +
  `(cfxId, iconVersion)` icon cache; `SavedServerItem` enrichment state; `MainViewModel`
  capture/refresh loop; row XAML converters) and closed them all.