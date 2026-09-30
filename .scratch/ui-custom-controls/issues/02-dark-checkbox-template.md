# 02: Dark CheckBox template adopted at every checkbox

**What to build:** All five checkboxes (settings auto-launch, add/edit dialog requires-Steam +
requires-Discord, browser hide-full + hide-empty) render as dark boxes matching the theme:
panel-light fill with deep border; when checked the box stays dark and shows an amber check
glyph with an amber border; hovering brightens the border; disabled dims to ~0.45 opacity.
Clicking anywhere on the control — box or label — still toggles; the two-state bindings are
untouched.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 723 green; visually verified by the user — checked keeps a deep border, grey on hover/focus)

- [ ] All five checkboxes render dark: panel-light fill + deep border
- [ ] Checked = dark box with amber check glyph + amber border; unchecked shows no glyph
- [ ] Hover brightens the border; disabled dims to ~0.45 opacity
- [ ] Clicking the label (not just the box) still toggles
- [ ] `IsChecked` bindings (AutoLaunch, DialogRequiresSteam/Discord, HideFull/Empty) unchanged
- [ ] Build green, full suite green
- [ ] Manual visual verification on all five sites

**Spec:** `.scratch/ui-custom-controls/spec.md`
