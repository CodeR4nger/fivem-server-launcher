# 10: Catalog iconVersion is a typed proto field, not a var (ticket 05 regression)

**What broke:** Ticket 05 wired icon resolution to the catalog snapshot through an assumed
`vars["iconVersion"]` entry. The live streamRedir payload carries iconVersion as a typed proto
field (`ServerData` field 11, int64) and never publishes it as a var. After the `/single/`
probe path was deleted, no row in the saved list or the browser could ever resolve an icon
version, so icons stopped loading everywhere.

**Evidence (live payload, 19.7 MB / ~33k servers, 2026-09-29):** zero occurrences of the string
"iconVersion" as a var key; field 11 (varint) present on ~97.5% of servers; field-11 values
match `/single/` `iconVersion` exactly (5dg4mr=512213334, yjbqg5=40388374,
6mmzz48=402313838, 5oovlp7=-208647301, kqmb4ed=-2084130510). Semantics (user-confirmed, and
verified live): **any non-zero `iconVersion` — including negative — means the server has an
icon** (`/icon/{id}/{version}.png` returns 200 + PNG magic for positive AND negative
versions); only zero (field absent) means no icon. A version that unexpectedly 404s degrades to
a null icon in the download step (existing `DownloadIconAsync` outage behavior).

**Blocked by:** None.

**Status:** done

- [x] `Proto/master.proto` carries the typed `iconVersion` field (field 11) with the semantics
      documented
- [x] Presence + browser rows read the typed field through one shared mapper
      (`CfxVars.TryGetIconVersion`; `!= 0` = has icon, zero = no icon) so the loop and the
      browser cannot drift
- [x] Fixture-based tests reshaped to the real payload shape (typed field, negative-version and
      zero cases); live end-to-end smoke through production code only
      (`ServerEnrichmentService.RefreshAsync` + `GetIconAsync`) resolves versions for 12.5k
      servers and downloads PNG icons
