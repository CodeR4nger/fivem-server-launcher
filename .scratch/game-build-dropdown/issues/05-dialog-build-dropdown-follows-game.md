# 05: LOCAL DEV panel build dropdown following GAME

**What to build:** In the add/edit dialog's loopback-only LOCAL DEV section, the game build
TextBox becomes a combo whose list follows the GAME selection: FiveM builds for GAME = FiveM,
RedM builds for GAME = RedM, built from the session-fetched build data when it has arrived,
the curated baseline until then (and on outage). GAME = FiveM Enhanced disables the combo
(Enhanced ignores build flags), and GAME = None disables it too. Editing a loopback server
with a stored build loads it into the combo — selecting its entry when listed, showing a
raw-number entry when not — and keeps it unless the user picks another. Saving persists the
same nullable build on the saved server as today (schema unchanged); the dialog's validation
and the localhost connect semantics are untouched.

**Blocked by:** 03 (dataset shape + session service; independent of 04).

**Status:** ready-for-agent

- [ ] The build control is a combo bound per GAME: FiveM list, RedM list, disabled on Enhanced
- [ ] GAME = None disables the combo
- [ ] The option list uses the session-fetched build data once it lands, the curated baseline
      before that and on outage (selection is preserved across the swap)
- [ ] Editing a server with a listed build selects its entry; an unlisted stored build shows a
      raw-number entry and survives a no-change save
- [ ] Saving persists the selected build (or None) to the saved server exactly as the TextBox
      path does today
- [ ] Switching GAME with a build selected never silently clears it (unlisted → raw entry)
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/game-build-dropdown/spec.md`

## Comments

- Recorded decision: GAME = None disables the combo (treated like Enhanced — no manual game
  client means no game to pick a build for). Confirmed with the user at ticketing time.
