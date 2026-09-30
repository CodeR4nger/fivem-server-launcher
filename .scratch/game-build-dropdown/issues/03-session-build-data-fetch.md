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

**Status:** ready-for-agent

- [ ] Numbers parse from the premake Lua (five/rdr3 sections; other sections ignored) and
      come out newest-first
- [ ] Names parse from the frontend bundle's `getGameBuildDLCName` switch (hash-resolved
      URL), mapping case-fallthroughs (2189/2215/2245) to one name
- [ ] The two sources merge into one dataset the option engine consumes unchanged
- [ ] Outage, malformed source, or format drift on either source → no update, never throws
- [ ] Fetched once per session (composition-root owned; not per dropdown use)
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/game-build-dropdown/spec.md`
