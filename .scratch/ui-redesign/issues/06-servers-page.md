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

**Status:** ready-for-agent

- [ ] Filter chips and counts tested (Offline = not online, including not listed); filter composes with search.
- [ ] `IsReorderAvailable` requires no search AND All; existing reorder tests still pass.
- [ ] Selection rules tested (first row on start, neighbour after delete, none only when empty); selecting fills the address.
- [ ] Copy address goes through a seam and is tested; Move up/down reuse the existing commands.
- [ ] The old row kebab, row context menu and their close-guard code-behind are removed; tests that named them are rewritten.
- [ ] Layout test for the page at minimum size in all 11 languages (long names, long builds, three requirements) passes.
- [ ] Manual check: drag reorder, menu from mouse and keyboard, Esc closes menu; screenshots recorded; zero warnings.
