# 05: Close the reorder-saved-servers phase

**What to build:** The two-axis review pass the phase is owed, plus the documentation that makes
the feature discoverable to the next session. Review the reorder diff against the repo's
standards and against the spec on both axes, fix what the review finds, then record the feature
in the places that describe the current state: the repo's current-state notes, the roadmap
(tick off the reorder line, keep the `ui-custom-controls` ordering note honest), and the
changelog.

**Blocked by:** 01, 02, 03, 04.

**Status:** resolved (suite 723 green; reviews recorded below; docs updated)

- [x] Two-axis review (repo standards + spec) over the phase diff (fixed point `539c13e`), both
      axes reported separately
- [x] Findings triaged: the two cosmetic standards nits fixed in-session (missing blank line,
      missing trailing newlines); the spec flag is handled in the docs step (below)
- [x] Suite green after any fixes
- [x] `AGENTS.md` current state describes the reorder behavior and its two invariants
      (row-addressed moves; no reordering under an active filter)
- [x] The roadmap marks the feature (v1.3 phase 14) delivered; remaining v1.3 items listed
- [x] The changelog entry is user-visible phrased under a new `1.3.0 (unreleased)` section

## Comments

**Standards axis:** clean, no hard violations — the review confirmed the VM/seam split, the
filter-order invariant, the row-addressed-command pattern, the position-only `Move` seam and the
test-rename rule. Judgement calls accepted as-is: the two arrow buttons duplicate the pencil
template idiom (deferred to the `ui-custom-controls` feature per the agreed sequencing), the
`Move`/`Replace` duplication in the file + in-memory repositories mirrors the pre-existing
Add/Update/Remove duplication, and `CancelDrag` is a one-line alias named for call-site intent.
Its one flag — a `CHANGELOG.md` diff containing unrelated pending v1.2 localization lines plus
no reorder entry — is exactly the pending housekeeping recorded before this phase: the
localization lines were already refined when this phase started and stay together in the v1.2
section; the reorder entry was added under a new v1.3 section.

**Spec axis:** implementation matches the spec; story 2's "disabled (hidden)" boundary arrows
landed as dimmed-disabled (the loosest acceptable reading; visually approved). The only coupling
beyond the spec's letter is ticket 02, justified by story 5 ("nothing regresses") and mandatory
for the move seam to behave (an edit left the store and the list in different orders, which would
have made index-based moves land wrong after an address change).

The visual placement needed three iterations; the empirical note for future overlays: children of
a ScrollViewer cannot render into the content margins of parents (negative-margin overlays get
clipped) — the panel margin was redistributed so the gutter lane is real layout space.

## Comments

- Fixed point for the review diff: the commit before ticket 01 lands.
- The spec's own note ("custom control styling from the UI-polish feature should be applied to
  any new buttons") is deferred by agreement: the arrows ship with the existing pencil idiom and
  `ui-custom-controls` restyles them later. Worth stating in the close-up so it does not read as
  a missed criterion.
