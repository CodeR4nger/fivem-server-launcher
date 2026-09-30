# 07: Server browser full-width slide

**What to build:** The server browser overlay stops being a jump cut: it slides in across the
full window width, right to left, on open, and slides back out, left to right, on close — a
real navigation push/pop (user-refined from a short nudge+fade: the full travel must be
visible). The travel distance is the live window width, which a style storyboard cannot
animate (style storyboards freeze their animations, so their To cannot bind), so the slide
lives in the view's code-behind as visual glue, driven by the VM flag through
`IsServerBrowserOpen` — the flag stays the single source of truth; only visibility, the
transform and hit-testing are touched. Both motions are To-only (~300 ms ease-out), so
closing from mid-slide-in reverses smoothly and reopening mid-close resumes from the live
position; clicks pass through the overlay once the flag drops, and the overlay collapses on
its own exactly when the exit completes — never stuck open.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 742 green; visually verified by the user after one refinement: the full-width slide reversed to enter from the left edge. The travel distance is the live window width, so the motion is code-behind glue — style storyboards freeze their animations and cannot bind a dynamic To)

- [ ] Browser slides in across the full window width, right to left, on open (~300 ms,
      ease-out)
- [ ] Close slides back out, left to right, and the overlay collapses on its own exactly at
      completion
- [ ] Closing from mid-slide-in reverses smoothly; reopening mid-close resumes from the live
      position — no snap, no flicker, no stuck overlay
- [ ] The VM flag remains the single truth; browser state (filters, rows, IsSaved markers)
      unaffected
- [ ] Clicks pass through the outgoing overlay once the flag drops; the idle overlay is
      truly collapsed (not focusable, not hit-testable, never always-mounted)
- [ ] The travel distance adapts to window resizes (measured per transition)
- [ ] Build green, full suite green
- [ ] Manual visual verification: open, close, rapid close/reopen, mid-motion reversals,
      tab focus stays out of the hidden browser

**Spec:** `.scratch/ui-custom-controls/spec.md`
