# 04: Drag a saved-server row to a new position

**What to build:** A saved-server row can be picked up and dropped at a new position, so several
servers can be rearranged in one gesture. Pressing a row starts a drag, the row follows the
pointer, and dropping it on a target slot reorders the list exactly as the arrows would, then
persists. The view owns nothing but the visual mechanics — capture, the dragged-row feedback and
computing which slot the pointer is over — and hands the result to the view model, which performs
the move (and refuses it while a search filter is active). Dragging must not interfere with
clicking a row to select it or reaching its edit pencil.

**Blocked by:** 01.

**Status:** resolved (suite 723 green; drag feedback visually verified by the user)

## Answer

First pass had drag with only a cursor cue. Visual verification asked for a more visible drag
state, so the drag now dims the dragged row (55 % opacity) and paints an accent drop line at the
exact insertion slot, following the pointer (an overlay `Canvas` over the list, hit-test
invisible). The line carries real semantics: it marks the insertion point (above/below the row
under the pointer, or past the last row), and the view translates that insertion point into the
final index the view model expects (past the dragged row the target slot shifts by one — the only
arithmetic left in the view, and there is one explicit comment pinning why). All reorder rules
still live in the view model: row-addressed move, boundary no-ops, refuse-while-filtering (pinned
by tests).

- [x] Dragging shows unmistakable feedback — the dragged row dims and an accent line follows
      the pointer, marking the slot where the row will land
- [x] Dropping moves the row to the marked slot and persists the new order
- [x] A plain click on a row (no drag) still only selects it — system drag threshold
- [x] The arrows and the edit pencil remain usable (a press starting on a button never arms a drag)
- [x] A drag started while a search filter is active does not reorder anything (VM-gated, tested)
- [x] Dropping outside any row leaves the order untouched
- [x] No business logic in the view: handlers measure (start row, threshold, drop slot) and
      call the view model's move entry point
- [x] Build green, suite green
- [x] Visual verification by the user (drag feedback approved)

**Spec:** `.scratch/reorder-saved-servers/spec.md`

## Comments

- The reorder logic itself is ticket 01's and is unit-tested there; this ticket is the thin visual
  layer the repo allows in code-behind (pointer capture, drop index from container geometry).
- A threshold is needed between "click" and "drag", otherwise every selection click also starts a
  drag.
