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

**Status:** resolved

## Answer

Landed as the first consumer of the option engine + session service:

- **VM**: `DevGameBuild` changed from a parsing `string` to a plain `int?` selected-value
  (persisting immediately on change as before; None = null clears the override). The parsing
  logic simply vanished — the combo can only produce listed values or null.
  `DevGameBuildOptions` is a computed property over `GameBuilds.Options(DevClient, stored,
  localized None, _gameBuildData)`; `_gameBuildData` is the session field fed by the new
  `SetGameBuildData` (stores + raises; the composition root is its only caller, so the
  once-per-session semantics live in the root). Raises wired into: the `DevGameBuild` setter
  (a new selection can remove the raw stored entry), the `DevClient` toggle (FiveM ↔ RedM
  list switch), and `OnLanguageChanged` (the None label re-localizes — pinned with a Spanish
  switch test alongside the existing dialog one).
- **XAML**: the TextBox became a ComboBox mirroring the dialog's GAME combo idiom
  (`ItemsSource`/`DisplayMemberPath="Label"`/`SelectedValuePath="Build"`/`SelectedValue`);
  the phase-15 dark template and open animation are implicit, so it inherits them for free.
- **Composition root**: `GameBuildDataService` shares the app `HttpClient` (15 s timeout);
  a fire-and-forget task fetches once right after window show and pushes a non-null result
  through the window dispatcher into `SetGameBuildData`. Any failure (fetch null, dispatcher
  shutting down) is swallowed — the curated baseline already covers the UI.
- The old `SetDevGameBuildAndPureMode` test kept its name (its claim is unchanged) with the
  mechanical `string → int?` assignment update; five new tests cover None-clears-null, the
  baseline FiveM list, the RedM toggle (stored 3095 stays visible as a named raw entry — the
  engine's named-if-known behavior, no invented mapping, value untouched), the session-data
  swap with selection preserved + property raise, and the localized None.

Visual eyeball of the combo (dark style, animation, selection display) pending the user's
manual check, as with every XAML-touching ticket.

- [x] The Dev Mode build control is a combo bound to the option engine's list; the list
      switches with the CLIENT toggle (FiveM on Legacy, RedM on RedM)
- [x] The option list uses the session-fetched build data once it lands, the curated baseline
      before that and on outage (selection is preserved across the swap)
- [x] Picking None clears the override (persisted null), exactly as an empty textbox does today
- [x] Picking a build persists immediately; reopening Dev Mode shows it selected (round-trip
      through settings)
- [x] A stored build not in the current client's list renders as a raw-number entry and keeps
      launching with that number until changed
- [x] Toggling the client with a persisted build from the other list shows that build as a raw
      entry (no silent clearing, no invented mapping)
- [x] The launch path receives the same `int?` it does today — no launch-semantics change
      (`DevLaunchAsync` reads `_settings.DevGameBuild`, untouched)
- [x] Suite green (RED → GREEN → REFACTOR) — 5 new tests, full suite 761 green

**Spec:** `.scratch/game-build-dropdown/spec.md`

## Comments

- Recorded decision amending spec story 5 ("the Dev Mode build dropdown offers the FiveM
  list"): the dropdown follows the CLIENT toggle instead. Rationale: story 4 establishes the
  follow-selection pattern and story 5's own "so both surfaces work the same way" points the
  same way; a strictly-FiveM list would make a RedM-toggled session unable to pick a valid RedM
  build while a persisted FiveM build applied to RedM launches. Confirmed with the user at
  ticketing time.
