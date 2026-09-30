# 04: Dev Mode build dropdown

**What to build:** In Dev Mode, the game build is picked from a dropdown instead of typed as a
raw number. The TextBox becomes a data-bound dark combo (the existing combo template and open
animation) listing the builds for the currently toggled client — the FiveM list on Legacy, the
RedM list on RedM — built from the session-fetched build data when it has arrived, the curated
baseline until then (and on outage). The persisted build setting stays an `int?` (null = None)
and keeps saving immediately on selection as today; a stored build selects its entry, a stored
build outside the current list shows as its raw-number entry. Nothing about how the launch
consumes the build changes.

**Blocked by:** 03 (dataset shape + session service).

**Status:** ready-for-agent

- [ ] The Dev Mode build control is a combo bound to the option engine's list; the list
      switches with the CLIENT toggle (FiveM on Legacy, RedM on RedM)
- [ ] The option list uses the session-fetched build data once it lands, the curated baseline
      before that and on outage (selection is preserved across the swap)
- [ ] Picking None clears the override (persisted null), exactly as an empty textbox does today
- [ ] Picking a build persists immediately; reopening Dev Mode shows it selected (round-trip
      through settings)
- [ ] A stored build not in the current client's list renders as a raw-number entry and keeps
      launching with that number until changed
- [ ] Toggling the client with a persisted build from the other list shows that build as a raw
      entry (no silent clearing, no invented mapping)
- [ ] The launch path receives the same `int?` it does today — no launch-semantics change
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/game-build-dropdown/spec.md`

## Comments

- Recorded decision amending spec story 5 ("the Dev Mode build dropdown offers the FiveM
  list"): the dropdown follows the CLIENT toggle instead. Rationale: story 4 establishes the
  follow-selection pattern and story 5's own "so both surfaces work the same way" points the
  same way; a strictly-FiveM list would make a RedM-toggled session unable to pick a valid RedM
  build while a persisted FiveM build applied to RedM launches. Confirmed with the user at
  ticketing time.
