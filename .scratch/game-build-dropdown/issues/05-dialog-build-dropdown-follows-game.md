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

**Status:** resolved

## Answer

Landed as the second consumer of the engine + session service, mirroring ticket 04's shape:

- **VM**: `DialogGameBuildText` (parsing string) became `DialogGameBuild` (`int?`) — the save
  path's `int.TryParse` simply vanished (a combo cannot produce garbage); open-add resets to
  null, open-edit loads the stored build, save passes the value straight through, so the
  saved-server schema and localhost connect semantics are untouched.
- **Follow-GAME list**: `DialogGameBuildOptions` computes from the GAME selection — FiveM and
  RedM produce the engine's list (session data, baseline until it lands); GAME = None and
  GAME = Enhanced produce an empty list with `IsDialogGameBuildEnabled` false, which the
  combo binds for the disabled state (the two recorded decisions).
- **Raises wired**: the `DialogGameBuild` setter (a changed value can add/remove the raw
  entry), the `DialogGameClient` setter (list + enabled flip together), `SetGameBuildData`
  (session refresh, alongside the Dev Mode raise), and `OnLanguageChanged` (localized None —
  pinned with a Spanish switch test).
- **XAML**: the TextBox became a ComboBox with the dialog's combo idioms (MinHeight 30,
  matching margins) plus `IsEnabled` binding; the implicit phase-15 dark template and open
  animation apply.
- One expectation corrected during the loop: the unlisted stored build shows as the **bare**
  number `2215` under the curated baseline — the `2215 — Cayo Perico Heist` label is
  live-bundle knowledge that only exists once the session fetch merges it (pinned at the
  service level in ticket 03); a VM test without session data must expect the baseline
  behavior. The no-clear-on-GAME-switch test uses the baseline-named 3095 to pin that a
  switched-away value keeps both its slot and its label.
- Tests: 5 new (follow-GAME walk-through, no-clear-on-switch, session refresh, unlisted
  edit + no-change save, localized None) + 3 mechanical `string → int?` updates to the
  existing dialog tests (claims unchanged). Suite 766 green.

Visual eyeball of the dialog combo (disabled states, following GAME) pending the user's
manual check.

- [x] The build control is a combo bound per GAME: FiveM list, RedM list, disabled on Enhanced
- [x] GAME = None disables the combo
- [x] The option list uses the session-fetched build data once it lands, the curated baseline
      before that and on outage (selection is preserved across the swap)
- [x] Editing a server with a listed build selects its entry; an unlisted stored build shows a
      raw-number entry and survives a no-change save
- [x] Saving persists the selected build (or None) to the saved server exactly as the TextBox
      path does today (schema unchanged)
- [x] Switching GAME with a build selected never silently clears it (unlisted → raw entry)
- [x] Suite green (RED → GREEN → REFACTOR) — 5 new tests, full suite 766 green

**Spec:** `.scratch/game-build-dropdown/spec.md`

## Comments

- Recorded decision: GAME = None disables the combo (treated like Enhanced — no manual game
  client means no game to pick a build for). Confirmed with the user at ticketing time.
