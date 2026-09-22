# UI overhaul follow-ups: layout fixes + add/edit server dialog

**Status:** done

## Problem Statement

Current build has visible layout regressions and a missing edit flow:

1. The settings and dev mode buttons' texts are trimmed (fixed heights too small for the style's
   font-size/padding).
2. The dev-mode panel clips its bottom half — content overflows the fixed-height region.
3. Opening the settings panel while Dev Mode is on fails (toggles stack incorrectly).
4. The saved-servers list clips the top of the first row (padding/border eats the first item).
5. The connect-address textbox is clipped at the bottom (height smaller than its padding + font).
6. The list scrollbar uses the default WPF look — off-theme against the dark surfaces.
8. There's no way to edit a saved server: players currently delete + re-add. We also want an
   add/edit **dialog** (overlay) hosting name, address, Requires Steam, Requires Discord.

## Solution

- Fix all layout drains: replace absolute `Height`/`Padding` combos that collide, make the dev and
  settings cards size-to-content, and use min-heights instead of fixed heights where text needs
  room to breathe.
- Overlay dialog for add/edit server (in-window, keeping the dark frame) with fields: Name,
  Address, Requires Steam checkbox, Requires Discord checkbox, SAVE / CANCEL. In the saved-servers
  list each row has a small edit (✏) button that only appears on the **selected** row (hover turns
  it the accent colour). The Add row disappears (add button kept **under** the list — user approved
  this placement over the header suggestion); delete-button retention decided in the dialog — CANCEL
  closes.
- Identity: `Address` remains the repository key (edit-address = remove old + add new internally).
- Scrollbar: thin (`6` wide), transparent track, `#3A3A3A` thumb — matching the dark style.

## User Stories

1. As a player, I want all button labels fully visible in the tool bar, so the app reads properly.
2. As a player, I want the dev panel to show its full contents without clipping.
3. As a player, I want the settings panel to take priority over dev mode when the two toggles
   coexist.
4. As a player, I want the first item of my saved list readable fully.
5. As a player, I want the connect box entirely visible.
6. As a developer, I want the scrollbar style to match the dark UI.
7. As a player, I want to edit a saved server (all its fields) from its row.
8. As a player, I want the edit dialog to surface save/cancel and validation errors clearly.
9. As a developer, I want the same Repository-based add/update semantics reused
   (address-keyed identity) for the edit flow.

## Implementation Decisions

- New VM surface: `IsServerDialogOpen`, `EditingServer` (original server when editing, null for a
  fresh add), `DialogServerName` / `DialogServerAddress` / `DialogRequiresSteam` /
  `DialogRequiresDiscord`, `OpenAddServerDialogCommand`, `OpenEditServerDialogCommand`,
  `SaveServerDialogCommand`, `CancelServerDialogCommand`, plus `ServerDialogTitle` and
  `DialogError` (in-dialog validation/`Server already saved` messages, kept off `StatusText` so
  they aren't hidden behind the overlay).
- VM uses repository `Remove` + `Add` when address changes; plain `Update` otherwise (all address
  comparisons case-insensitive via `MatchesAddress` / `OrdinalIgnoreCase`).
- Dialog validation: same guardrails as `SavedServer.Create` (throws `ArgumentException`) → status
  text "Invalid name or address" surfaced in place.
- Top bar toggles: opening SETTINGS sets `IsDevMode = false` (mutually exclusive panels);
  DEV MODE toggles `IsSettingsOpen = false`.
- Saved servers list scrollbar: window-level `ScrollBar` style override (thin track/thumb), scoped
  to the user control so we don't touch app scope.

## Testing Decisions

- VM tests (existing style): dialog open/close state, editing-address semantics, validation,
  mutually-exclusive panel toggles.
- No need to test XAML wiring at suite level; manual visual verification remains acceptance medium.

## Out of Scope

- Deeper theming/animations; re-architecture of the panels (kept as simple cards).
- Server-info rich UI (separate independent item).

## Further Notes

- User confirmed: dialog is an in-window overlay; edit trigger is an inline pencil on each row;
  scrollbar is thin dark.
