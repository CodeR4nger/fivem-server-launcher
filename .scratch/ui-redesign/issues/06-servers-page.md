# 06: Servers page

**What to build:** the list + detail page from the mockup. Left: title with "+" (add) and
refresh (existing cooldown), search, All / Online / Offline chips with live counts, and rows
with server mark (icon or initials), name, filled game tag, status dot and players/Offline/Not
listed. Right: the selected server's detail — large mark, name, tag, status line, cards for
players, address, game build and pure mode (ticket 02's data), requirements chips — and a ⋮
menu with Edit, Create desktop shortcut, Copy address, Move up, Move down, Delete (existing
row-scoped commands plus Copy address through a clipboard seam). Bottom: the address field and
the amber Connect button with the status line (busy pulse). Drag-to-reorder with the drop line
stays (code-behind glue delegating to the view model); reorder is available only with no
search and the All chip. Empty state for no saved servers (points to "+" and Browse) and for no
search match. A server is always selected while the list is not empty. The detail slides in
on selection change; rows ease hover/selection.

See `../spec.md` (Saved-server filter chips, Selection, Actions menu).

**Blocked by:** 02, 04.

**Status:** done

- [x] Filter chips and counts tested (Offline = not online, including not listed); filter composes with search.
- [x] `IsReorderAvailable` requires no search AND All; existing reorder tests still pass.
- [x] Selection rules tested (first row on start, neighbour after delete, none only when empty); selecting fills the address.
- [x] Copy address goes through a seam and is tested; Move up/down reuse the existing commands.
- [x] The old row kebab, row context menu and their close-guard code-behind are removed; tests that named them are rewritten.
- [x] Layout test for the page at minimum size in all 11 languages (long names, long builds, three requirements) passes (only two requirements exist: Steam and Discord; both are shown).
- [x] Manual check: drag reorder, menu from mouse and keyboard, Esc closes menu; screenshots recorded; zero warnings.

## Comments

**Delivered (2026-11-16)** on `claude/ui-06-servers-page-99f5d8`.

### View model (tested at the seam)

| New member | What it does |
|---|---|
| `ServerFilter` (`ViewModels/SavedServerFilter`: All, Online, Offline) | the chips; composes with `ServerSearchText` in the one `SavedServersView` filter |
| `AllServerCount` / `OnlineServerCount` / `OfflineServerCount` | live counts over the whole saved list (the search does not change them); Offline = every row that is not online, not-listed and manual rows included; re-raised on add/delete and on any row's `Online` change, which also re-filters while a chip is active |
| `HasNoSavedServers` / `HasNoServerMatch` | the two empty states |
| `CopyAddressCommand` | row parameter or the selection → `Service/IClipboardService.TrySetText` (real `WpfClipboardService`; new optional ctor param `clipboard`, unwired = no-op); status "Address copied" / "Could not copy address" |
| `IsReorderAvailable` | now also requires the All chip |
| `SavedServerItem.ListStatusText` / `PresenceLabel` / `HasRequirements` | the row status (players while listed, else Offline / Not listed / Manual settings), the detail status line ("Online" while listed), the "None" requirement state |

**Selection rule** (`EnsureSelection`): a visible list always has a selected server. First row on start (so the
address field is filled at start, as a click would); the visible neighbour after a delete (next, or previous for the
last row); the added row after a dialog add; the edited row after an edit (it is rebuilt); the first visible row when
a chip or the search hides the selection; none only when nothing is visible. A browser save keeps the selection. A
`null` pushed by the list while the selection is still visible (Ctrl+click deselect) is refused and re-announced.
Selecting fills the address field, as before. One existing test named a state the rule made impossible
(`DeleteServerCommand_WithoutSelectionOrParameter_ShouldDoNothing`) and was rewritten as
`DeleteServerCommand_WithNoSavedServers_ShouldDoNothing`.

### Page

`Views/SavedServersPanel` became `Views/ServersPage` (git rename). List column `37*`, min 300, max 460 (≈338 px at
1000×640); the detail pane takes the rest. Chips are RadioButtons in the theme's `FilterChipStyle` bound through a
new `Views/EnumEqualityConverter`; status dots use a new `Views/ServerDetailStateToBrushConverter` (the CFX status
colours: listed green, manual amber, offline red, not listed grey). Detail cards wrap their values (the longest
build label, "2060 · Los Santos Summer Special", wrapped rather than trimmed at the minimum size); on a maximized
window the card columns cap at 364 px. Page-specific styles (row, card value, requirement chip, busy dot, detail
content) live in the page's resources; nothing new went into `Theme.xaml` except the `ServerMarkFontSize` converter
instance used by the shared mark template.

**Removed:** the row kebab and its 400 ms close guard, the row right-click `ContextMenu` with its `PlacementTarget.Tag`
bindings and `IsOpen` watcher, the gutter move arrows and their tracking code, the BROWSE / NEW SERVER buttons under
the list (Browse is a tab; "+" is in the title; the empty state has Browse servers). `SelectionEqualityToVisibility`,
`InverseBoolean` (now unused by any XAML) and the keys `SavedServersHeader`, `BrowseButton`, `NewServerButton`,
`ConnectAddressHeader`, `EnterServerButton` and `TrimmedName` on the row are left for ticket 10's cleanup.

**Actions menu:** opened by ⋮ (placed under it, right edges aligned) or by the keyboard context-menu gesture (Apps,
Shift+F10) on the list through `ContextMenuOpening` with no cursor position — a right-click opens nothing. Arrows move
inside it, Esc closes it and focus returns to the row. Move up / Move down use the existing commands and are enabled
by the selected row's `CanMoveUp` / `CanMoveDown`.

**Keyboard:** arrows move the selection (the detail follows), Enter on a row or in the address field connects.
Rows expose their name to UI Automation.

**Server-mark initials:** the shared template's `Viewbox` (shrink-to-fit, so "WW" rendered smaller than "LS") is
replaced by a fixed size, `ServerMark.InitialsFontSize` = 0.4 × the mark's height, through
`ServerMarkFontSizeConverter`. Tested: the helper, the converter, and a rendered 36 px mark where "WW" and "LS" get the
same font size, no Viewbox, and "WW" fits.

**Motion** (all on the switch, tested on and off): the detail slides/fades in on a real selection change, rows ease
their hover and selection layers, the busy dot pulses while busy, and the post-move row slide now snaps with motion
off. The busy pulse first used a `MultiDataTrigger` binding to `Motion.IsEnabled`, which never fired; it is a property
`MultiTrigger` on Visibility + the switch.

### Found in the real-app check (fixed, `ecc96b7`)

- **⋮ did not toggle:** a press outside the open menu closes it on capture loss *before* the press reaches any element
  (logged: the menu never sees it, the button sees it with the menu already closed), so a click on ⋮ reopened it — and
  the planned `IsHitTestVisible` binding could not help. Now the window's first press while the menu is open (tracked by
  `Opened` / `Closed` / window `Deactivated`) is the dismissing one and is swallowed on ⋮. Event-ordered, no time window.
- **Apps key opened nothing** from a `PreviewKeyDown` handler; `ContextMenuOpening` (keyboard-only) works.
- **Drag moved the selection** onto every row under the pointer, because the list held the mouse capture. The page now
  holds it; `LostMouseCapture` bubbles, so only the page's own loss ends a drag (the first try ended every drag at once).

### Theme bug (shared, not fixed here)

`TextInputStyle` applies `Padding` twice (its content-host margin plus the TextBox's own host padding), so typed text
sat ~17 px right of its placeholder. Ticket 07 found the same. This page passes half the inset and places placeholders
at 2 × Padding + 3; the theme fix belongs to ticket 10.

### Layout

`ServersPage_AtMinimumSize_ShouldClipNothing` (11 languages): long names (the only texts allowed an ellipsis), the
longest build label, Steam + Discord, a long domain; every element fits its slot and region, and the list column is
320–360 px. It failed first on the build label (cut), which led to wrapping card values; the shared clip check now
skips wrapping texts' natural width (their slot height is still checked). `ServersPage_WhenLarger_ShouldStretchBothColumns`
checks both columns grow at 1920×1040 with the list capped at 460.

### Screenshots (`../screenshots/06-servers-page/`)

Offscreen renders of the real window (throwaway probe, not committed; motion off): `min-size`, `maximized`
(1920×1040), `chip-online`, `chip-offline` (All is `min-size`), `menu-open`, `detail-offline`, `detail-not-listed`,
`detail-manual`, `long-name`, `empty-no-servers`, `empty-no-match`, `status-invalid-address`, `es-min-size`,
`de-min-size`. Live captures of the Debug exe: `runtime-start`, `runtime-drag-drop-line`, `runtime-menu-keyboard`,
`runtime-menu-mouse`, `runtime-invalid-address`.

### Real-app check

Debug exe, scratch `saved-servers.json` (four fake servers, no real ids) and `launcher-settings.json` (`AutoLaunch`
false) beside it, run only while no other launcher was running (one run waited for the Browse session's instance to
exit). Driven by real mouse input (`SetCursorPos`/`mouse_event`), keyboard (`keybd_event`) and UI Automation:

- drag row 1 below row 3 → the drop line marks the slot, the dragged row dims, the file's order changes, the dragged
  row stays selected;
- Apps and Shift+F10 open the menu, Down moves inside it, Esc closes it and focus is back on the row;
- ⋮ opens, a second click closes, a third opens;
- Copy address → the clipboard holds the selected address, the status says "Address copied";
- "not a server!!" + Enter → the status line says "Invalid address";
- the app exits 0 on close. Scratch data deleted afterwards. No server was connected to.

### Verification

Suite 1497/1497, green on 3 consecutive full runs after the last change (plus the runs in between).
`dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings, 0 errors. New keys (24) in all 11 languages,
Spanish curated.

### Not verified

- A second monitor or a DPI other than 100 % (the menu's custom placement is relative to the button, so it should hold).
- A screen reader reading the chips and the detail (UI Automation names were checked for rows and buttons only).
- The motion feel by a person; the motion paths are covered by tests on and off.
