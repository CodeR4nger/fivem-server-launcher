# 01: Persist a new saved-server order (seam + move commands)

**What to build:** A saved server can be nudged to a new position and that position survives a
restart. The server store gains a reorder seam that rewrites the saved list in the order it is
given (both implementations, no schema change), and the main view model gains bounds-safe
"move up" / "move down" commands plus a move-to-position entry point for dragging. Every entry
point addresses the **row**, never a raw index, so a filtered view can never move the wrong
server; each one persists the new order through the store. Moving past a boundary is a no-op,
and every move is refused while a search filter is active, because the visible order is then a
non-contiguous subset of the real one. No XAML in this ticket — the behaviour is verifiable end to
end: move a row, and the list order and the persisted order both change and survive a reopen.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

The store seam landed as a **position-only move** — `IServerRepository.Move(address, newIndex)` —
not the full-order rewrite the ticket first sketched ("rewrites the list in the given order").
The rewrite shape was rejected mid-implementation: the view model's rows can lag behind the store
(a cfx id captured in the background lives in the store while the row still holds `null`), so a
content-carrying reorder would have silently overwritten store fields it knew nothing about. A
position-only move can reorder anything (N-1 moves reach any permutation) but can never clobber
a field. The set-equality guard evolved into the equivalent capability-at-the-seam guards:
unknown address → `ArgumentException`, out-of-range index → `ArgumentOutOfRangeException`, both
leave the file untouched (pinned by tests).

View model surface: `MoveServerUpCommand` / `MoveServerDownCommand` (row-parameter commands),
`MoveServerToIndex(row, newIndex)` (public drag entry point that the relative arrow commands
share), `IsReorderAvailable` (gate: more than one row AND no search filter; notified when the
search text changes) and per-row `CanMoveUp` / `CanMoveDown` flags the view model recomputes on
every structural change (load, add, delete, edit rebuild, move, filter change). The store is
persisted first, the visible collection second, so a rejected store write never renders an order
the store does not have.

While wiring this up, a pre-existing inconsistency surfaced that made the move seam subtly wrong:
the edit dialog's address-change path removed and re-added the server in the *store* (appending
it) while the *list* preserved the row's slot — so a "move to N" computed against the visible
order could persist a different slot. Fixed as ticket 02; the two tickets became coupled and 02
was implemented inside this session.

- [x] The store reorders on demand and reading it back returns the new order (in-memory and
      file-backed, both pinned)
- [x] A move against an unknown address or an out-of-range index is rejected and leaves the
      stored data untouched (no silent data loss)
- [x] Moving a row up or down swaps it with its neighbour in the list and persists the new order
- [x] Moving the first row up, or the last row down, changes nothing (no exception, no write —
      the no-write is pinned at the store)
- [x] Move-to-position (the drag entry point) produces the same result and persists it
- [x] Moves address the row, so a row hidden by the search filter is never moved by mistake
      (`IndexOf` on the row, never a view index; the filter case is also gated shut)
- [x] Every move is a no-op while a search filter is active (list and store unchanged), and the
      commands report `CanExecute == false` for the XAML to bind to
- [x] The order survives reopening the store (move pinned through a fresh repository instance)
- [x] Suite green (RED → GREEN → REFACTOR)


**Spec:** `.scratch/reorder-saved-servers/spec.md`

## Comments

- Seam shape: the store rewrite takes the full ordered list of saved servers (not a
  "move this address to index N" instruction) — the store already persists list order, so order
  is the only thing being expressed. The set-equality guard is what keeps a caller bug from
  silently deleting a server the user saved.
- Row-addressed moves are deliberate: the list is bound to a filtered view, so a view index and
  a collection index are not the same number once a filter is active. A move that resolves the
  row to its position in the underlying list cannot pick the wrong server.
- The drag entry point lives here (ticket 01), not in ticket 04: the view's drag handling is thin
  glue, and the reorder behaviour it calls is the same behaviour the arrows exercise, so it is
  tested here once instead of twice.
