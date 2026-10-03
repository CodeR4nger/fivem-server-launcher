# 02: Row action menu: kebab, right-click, delete confirmation

**What to build:** Saved-server rows get one action menu that replaces both the edit pencil
and the footer DELETE button. Right-clicking anywhere on a row — or left-clicking a ⋮ kebab
on the selected row — opens a dark context menu: Edit server / Delete server. Deleting asks
for confirmation through a new, reusable confirm overlay. This ticket builds the
confirm-overlay mechanism that later features reuse.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Right-click anywhere on a row selects that row and opens the context menu at the cursor
- [ ] Left-click on the selection-only ⋮ kebab opens the same menu; the edit pencil is gone
- [ ] Menu order: Edit server / Delete server (a third item arrives in ticket 03); destructive
     action last, uniform dark styling — red stays reserved for failure status
- [ ] Edit server opens the existing edit dialog for the right-clicked row, unchanged
- [ ] Delete server removes the row only after confirmation; a misclick in the menu can never
     destroy a saved server
- [ ] The confirm overlay is a minimal sibling of the edit dialog (same overlay pattern,
     parameterized text + captured confirm action, CANCEL reuses the existing dialog key) —
     built as a reusable mechanism, not delete-specific
- [ ] The delete confirmation text includes the server name; CONFIRM deletes and clears the
     selection exactly as today's delete does
- [ ] The footer DELETE button is removed; BROWSE and NEW SERVER remain (the layout
     self-heals)
- [ ] Menu items and confirmation strings ship in all 11 languages (`en.json` source of
     truth; the completeness audit test stays green)
- [ ] View-model tests cover confirm state transitions, delete-after-confirm,
     cancel-dismissal; the XAML glue (menu open, right-click select, kebab positioning) is
     untested per house convention
