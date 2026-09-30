# 02: Editing a server no longer shuffles its position

**What to build:** Renaming a saved server or changing its address keeps it exactly where the
user put it. Today the edit path removes the server and re-adds it, which appends it in the
persisted list, while the on-screen list preserves the original position — so an edited server
jumps to the bottom of the list after a restart. Editing preserves the position in both the list
and the store. This is a precondition for reordering being worth anything: an order the user
arranged must not be undone by an unrelated edit.

**Blocked by:** None (can start immediately). (Landed together with 01 — the store's move seam
was only reliable end to end once this divergence was gone, so the two tickets coupled.)

**Status:** resolved

## Answer

New address-keyed seam `IServerRepository.Replace(address, replacement)`: the record under the
old address is swapped in place (even when the replacement carries a new identity), one atomic
store write. The edit dialog's address-change path now calls it instead of remove+add, so the
store and the on-screen list agree on the order after every edit. It also simplified the
three-way branch in the save path (the "same record case-insensitively" edge now just goes
through `Update`, which is already case-insensitive). The pre-existing test
`SaveServerDialogCommand_OnEditAddressChange_ShouldRemoveOldAndAddNew` named an implementation
the change made false; it was rewritten as `..._ShouldSwitchRowToNewAddress` with the observable
assertions kept, and the position contract got its own test
(`..._ShouldKeepTheRowInPlace`, list and store asserted alike).

- [x] Editing a saved server keeps its position in the list (no visible jump — the collection
      rebuild was already in place)
- [x] Editing a saved server keeps its position in the store — pinned by a round-trip through a
      freshly opened repository
- [x] Same for an edit that changes the address — that path is now `Replace`, one write
- [x] Case-insensitive same-address edit still goes through `Update`
- [x] Edit contract otherwise unchanged (flags, local-dev parameters, background capture, the
      forced refresh after save — the pre-existing edit suite stayed green untouched)
- [x] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/reorder-saved-servers/spec.md`

## Comments

- Found while designing ticket 01: the list and the store already disagree after an edit, and
  reordering would paper over it in one session and then drop the arrangement on the next start.
- The store's update is position-preserving by itself, so the fix is about the address-change
  path that has to remove + add, plus keeping the list and the store in agreement.
