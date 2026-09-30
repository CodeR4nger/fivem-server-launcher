# 03: Session build-data fetch service

**What to build:** The dropdown's data refreshes itself once per launcher session from CFX's
own sources, so a new GTA DLC appears with no launcher release. A small seam-based service
fetches (a) the supported-build numbers from the citizenfx/fivem `master`
`code/premake5_builds.lua` raw URL, parsing the five/rdr3 sections (ignoring any other
section, e.g. the GTA6-era `ny`), and (b) the number→DLC-name map from the CFX servers
frontend bundle — resolving the current content-hashed bundle file name from the page HTML
first, then mining the `getGameBuildDLCName` switch. Both merge into the same dataset shape
ticket 02's engine consumes. Anything that can go wrong — outage, malformed Lua, a
redesigned bundle, a moved file — degrades to no update (never throws); the UI keeps the
curated baseline. No UI in this ticket: the service is verifiable at its seam with faked
HTTP, and the composition root calls it once per session.

**Blocked by:** 02 (the dataset shape).

**Status:** resolved

## Answer

Landed as `Service/GameBuildDataService` (`IGameBuildDataService`, `HttpClient` seam, mirroring
the `CfxStatusService` pattern): one-shot `Task<GameBuildData?> FetchAsync()` — no cache, no
TTL; the once-per-session call is composition-root owned and lands with ticket 04 (the first
VM consumer).

Two source asymmetries emerged as the core design (amending the criterion that said "format
drift on either source → no update" — the spec's own line "degrades to the curated baseline"
is what shipped):

- **Numbers replace, all-or-nothing.** The premake Lua parse must yield a non-empty `five`
  AND a non-empty `rdr3` (either empty/missing/garbage → whole result null → UI keeps the
  baseline). Replacing matters: a build dropped from premake must disappear from the
  dropdown (it is no longer supported); a saved value still renders through the engine's
  unlisted-stored raw entry.
- **Names merge over the baseline, fetched wins.** The real bundle does not map `1604` or
  `1` — replacing would regress those labels on every successful fetch. Any name-path failure
  (page 404, no `serversList-*.js` reference, no `getGameBuildDLCName`, unparseable switch)
  degrades to the baseline names; numbers still refresh. The merge also picks up names the
  baseline lacks (the live bundle maps `2215`/`2245`/`3717`).

Parsers (both private, seam-tested through faked HTTP — no `InternalsVisibleTo` by design):

- Premake: line-based section state machine (`five`/`rdr3` matched by name; `game_(\d+)`
  entries; other sections like `ny` skipped; inline Lua comments irrelevant).
- Bundle: hash-resolved from the `/servers` page HTML (`src="(/assets/serversList-*.js)"`),
  windowed from `getGameBuildDLCName` to the function's `return``}` tail, then
  `case`N`` / `return`NAME`` token scan with pending-case fallthrough grouping.

Verified live (scratch console, real URLs): full merged dataset printed — 18 FiveM numbers,
4 RedM numbers, 21 merged names; a stored-but-unlisted `2215` renders as
`2215 — Cayo Perico Heist` through the engine's raw entry.

- [x] Numbers parse from the premake Lua (five/rdr3 sections; other sections ignored) and
      come out newest-first
- [x] Names parse from the frontend bundle's `getGameBuildDLCName` switch (hash-resolved
      URL), mapping case-fallthroughs (2189/2215/2245) to one name
- [x] The two sources merge into one dataset the option engine consumes unchanged
- [x] Outage, malformed source, or format drift: numbers-side → no update (null); names-side
      → baseline names; never throws
- [x] Fetched once per session (composition-root owned; not per dropdown use) — the
      composition-root call itself lands with ticket 04, the first consumer
- [x] Suite green (RED → GREEN → REFACTOR) — 6 new tests, full suite 756 green

**Spec:** `.scratch/game-build-dropdown/spec.md`
