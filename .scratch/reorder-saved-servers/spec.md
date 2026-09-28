# Feature: Reorder saved servers (arrows + drag)

Status: ready-for-agent

## Problem Statement

Saved servers render in insertion order and there is no way to reorder them; users cannot keep
their favorite or most-used servers at the top of the list.

## Solution

Rows reveal small up/down buttons on their left side when hovered (or selected), and rows can
also be dragged to a new position. Both paths reorder the underlying collection and persist the
new order to the saved-servers store, so it survives restarts.

## User Stories

1. As a user, when I hover a saved server row I want up/down arrows to appear at its left, so I
   can nudge it into place.
2. As a user, I want the arrows disabled (hidden) at the top/bottom boundary, so the list cannot
   scroll off.
3. As a user, I want to drag a row onto another position and have the list reorder, so bulk
   rearranging is fast.
4. As a user, I want the new order to persist across restarts, so I only arrange once.
5. As a user, I want reordering to work with everything else the row does (selection, edit
   pencil, enrichment presence, search), so nothing regresses.
6. As a user, while a search filter is active I expect reorder affordances to hide, so I never
   reorder against a filtered (non-contiguous) view.

## Implementation Decisions

- Order persistence: the JSON store already preserves list order; add a repository seam method
  that rewrites the list in the given order (the file-backed and in-memory implementations both
  implement it; no schema change).
- The main view model owns `MoveServerUp`/`MoveServerDown` (bounds-safe swaps on the observable
  collection + persist through the repository) and a drag-reorder entry point (move item from
  index to index with the same persistence). Rows expose commands that call into the VM
  (existing pattern: rows already reach VM commands via RelativeSource bindings).
- Arrow visibility: hover- or selection-triggered via XAML triggers; boundary rows disable the
  respective arrow.
- Drag-and-drop: thin visual code-behind in the view (mouse capture, drop index calculation)
  calling the VM reorder entry point — business logic stays in the VM per the repo rule that
  code-behind may only be pure visual glue.
- Reordering is disabled while the search text filter is active (arrows hidden; drag rejected).
- Enrichment, CfxId capture, edit/delete flows are order-agnostic and must be unaffected.

## Testing Decisions

- Good tests assert external behavior at VM and repository seams: move up/down reorders the
  collection and persists the new order (InMemory + file-backed round-trip), boundary moves are
  no-ops, drag-style move-from-index behaves identically, search-active state hides/disables
  the affordances.
- The drag visuals (mouse handling in code-behind) are manual-verification glue, not unit
  tested; the reorder logic they call is fully covered.
- Prior art: repository round-trip tests and the existing saved-server VM suites.

## Out of Scope

- Multi-select or bulk moves; drag reordering inside the server browser; reordering across the
  search filter; undo.

## Further Notes

- Runs after the UI-polish feature per the agreed order; the custom control styling from that
  feature should be applied to any new buttons the arrows introduce.
