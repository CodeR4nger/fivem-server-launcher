# 04: One shared IconButtonStyle for the glyph buttons

**What to build:** The five small glyph buttons — saved-list refresh, browser refresh, edit
pencil, move up, move down — stop carrying five copy-pasted inline templates and per-instance
hover styles and instead share one IconButtonStyle: transparent background, centered glyph,
glyph in secondary grey flipping to amber on hover, and one normalized disabled treatment
(~0.35 opacity dim). This deliberately changes the refresh buttons' cooldown look: instead of a
near-invisible glyph, a cooling-down refresh stays dimmed-but-visible; the move arrows keep
dimming at the boundaries the same way. Commands, tooltips, cursors and per-button explicit
sizes are unchanged — a pure visual consolidation leaving one clear place to restyle the idiom.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 729 green; visually verified by the user — one hover idiom and one normalized ~0.35 disabled dim across all five glyph buttons)

- [ ] One shared style owns template + hover + disabled for all five glyph buttons; no inline
      copies remain
- [ ] Hover feedback identical everywhere: secondary grey glyph flips to amber
- [ ] One disabled treatment (~0.35 opacity): refresh during cooldown is dimmed-but-visible,
      boundary arrows dim the same way
- [ ] Commands, tooltips, cursors and sizes unchanged; pencil/arrows visibility triggers
      untouched
- [ ] The duplicated inline XAML is removed
- [ ] Build green, full suite green
- [ ] Manual visual verification on all five buttons + the refresh cooldown state

**Spec:** `.scratch/ui-custom-controls/spec.md`
