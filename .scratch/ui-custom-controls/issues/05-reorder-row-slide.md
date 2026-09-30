# 05: Reorder row slide (arrows + drag)

**What to build:** Reordering a saved server — with the move arrows or by dragging — slides the
affected rows into their new slots instead of jump-cutting. After the collection move lays rows
out at their final positions, the moved row and every row it displaced render a
`TranslateTransform` from their pre-move offset and animate it to zero (~200 ms ease-out) —
pure visual glue in the existing reorder code-behind. Both paths funnel through the collection
`Move` and rows are fixed-height, so the deltas derive from the move indices and the measured
row height: no pre-move capture, no VM changes. Edge behavior: rapid successive moves stay
coherent (previous slide completes instantly or retargets — no stacked or stuck transforms); a
scroll during a slide cancels it; on drag drop the row un-dims and the drop line hides before
the slide; the move-arrows overlay and drop line target the final layout and never ride the
animation. Rejected/no-op moves and the filtered state (reordering unavailable) never animate;
persist-first, bounds and filter semantics untouched.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 729 green; visually verified by the user after one fix — the displaced-row range was inverted on the first pass, so adjacent arrow moves double-animated the moved row while its neighbor teleported; the corrected range gives the two-server swap the user asked for, with drag motion unchanged. Range math pinned numerically in a harness: adjacent up = moved +1 slot, neighbor −1; multi-slot drop = moved −N, displaced +1 each)

- [ ] Arrow moves slide the moved row + displaced rows into place (~200 ms ease-out), both
      directions, at the list boundaries
- [ ] Drag drop: un-dim + drop-line hide precede the row sliding into its slot
- [ ] Rapid repeated moves stay coherent — no stacked or stuck transforms
- [ ] Scrolling mid-slide cancels the animation cleanly
- [ ] Rejected/no-op moves and the filtered state never animate
- [ ] Arrows overlay + drop line target the final layout, never ride the animation
- [ ] No VM changes; reorder semantics (persist-first, bounds, filter rejection) untouched
- [ ] Build green, full suite green
- [ ] Manual visual verification: arrows up/down, drag across several slots, rapid clicks,
      scroll mid-slide

**Spec:** `.scratch/ui-custom-controls/spec.md`
