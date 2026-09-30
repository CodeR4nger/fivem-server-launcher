# 03: Up/down arrows on the saved-server row

**What to build:** Hovering (or selecting) a saved-server row reveals two small arrow buttons on
its left, so a server can be nudged up or down the list with the mouse — the behaviour the
reorder seam already provides. The arrows appear on the row's left without shifting the rest of
the row, take the same visual idiom as the existing edit pencil (transparent button template,
accent glyph on hover) so there is a single place to restyle later, and the whole column hides
while a search filter is active, because a filtered order is not the real order. The arrow that
would move a row past the top or bottom edge is disabled. Everything else the row does —
selection, edit pencil, presence, icon, game tag — keeps working.

**Blocked by:** 01.

**Status:** resolved (suite 723 green; visually verified by the user after two reworks)

## Answer

**First pass landed arrows as a leading column inside the row** — accepted in mechanism,
rejected in placement: they must not take space from the list.

**Second pass moved them to a 26 px gutter column inside the grid wrapping the list** — still
visually rejected: the list box itself must keep its original size.

**Landed (revision D): a gutter carved out of the space that *was* the list's left margin.** The
panel's content StackPanel no longer owns the 20 px left margin; every sibling element carries it
explicitly, and the list's grid occupies the full remaining width with columns `[20px arrows] +
[* list]`, so the list box renders at its exact original position and size. A negative-margin
overlay hung INTO that margin was tried first and is empirically clipped by the panel's
ScrollViewer — that space is not renderable overflow, so real layout space was the only correct
route (verified with a forced-visible probe before the final build; probe for selection/hover and
scroll positioning then confirmed by the user on the real list).

Behavior per row: arrows visible for the hovered row (or the selected row), positioned from
code-behind against the hovered container via `TranslatePoint`, re-aligned on hover/selection/
scroll changes; hidden while a search filter is active; boundary arrows disable via the row's
`CanMoveUp`/`CanMoveDown`; commands are row-addressed so a rebuilt row never breaks them.

- [x] Hovering a row reveals its up/down arrows in the margin left of the list box; the selected
      row's arrows stay visible
- [x] The list box and its rows keep their exact original width — the arrows overlay the margin,
      never the list
- [x] Clicking onto the arrows does not collapse them mid-click
- [x] Only the boundary arrow disables; both hide while a search filter is active
- [x] Clicking an arrow moves the row and the list re-renders in the new order
- [x] Selection, edit pencil, delete, icon, game tag and status still work on the reordered list
- [x] Build green, suite green
- [x] Visual verification by the user (revision D: arrows in the former margin space)

**Spec:** `.scratch/reorder-saved-servers/spec.md`

## Comments

- Bound to the view model's move commands with the row as the command parameter, exactly like the
  existing edit pencil reaches its command through the user control — a row-addressed command
  survives the row being rebuilt on edit.
- Command availability depends on collection position, so the view model must re-query command
  state after a move, the same way the saved-list refresh does after its cooldown.
- Arrow buttons follow the current pencil styling rather than waiting for the
  `ui-custom-controls` feature (agreed sequencing): that feature gets a single shared place to
  restyle later.
