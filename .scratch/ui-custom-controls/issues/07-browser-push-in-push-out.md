# 07: Server browser push-in/push-out

**What to build:** The server browser overlay stops being a jump cut: it pushes in from the
right on open (~80 px + fade, 250 ms ease-out) and plays the reverse on close — the one surface
that animates both ways. The VM flag stays the single source of truth for browser state; the
close is a small code-behind visual-glue step that plays the out-storyboard, collapses the
overlay on completion, and cancels (re-shows instantly) when the browser is reopened
mid-close. Deliberately NOT the always-mounted opacity-0 pattern — an invisible-but-visible
overlay would leak keyboard focus and hit-testing into hidden content; delaying only the
collapse keeps `Visibility` semantics intact.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Browser pushes in from the right on open (~80 px + fade, 250 ms ease-out)
- [ ] Close plays the reverse motion before the overlay collapses
- [ ] Reopening mid-close cancels the close and re-shows instantly — no flicker, no stuck state
- [ ] The VM flag remains the single truth; browser state (filters, rows, IsSaved markers)
      unaffected
- [ ] Hidden browser is not focusable or hit-testable (no tab-into-invisible)
- [ ] No always-mounted hidden overlay; `Visibility` semantics preserved
- [ ] Build green, full suite green
- [ ] Manual visual verification: open, close, rapid close/reopen, tab focus stays out of the
      hidden browser

**Spec:** `.scratch/ui-custom-controls/spec.md`
