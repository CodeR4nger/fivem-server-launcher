# 01: Dark ComboBox template adopted at every dropdown

**What to build:** Every non-editable dropdown in the app (settings preferred client, settings
language, dev pure mode, loopback-dialog pure mode + game, browser game filter) renders with a
dark custom template instead of the light Windows default. Closed state: panel-light fill, deep
border, white text, secondary chevron. Popup: panel-light background, deep border, the thin dark
scrollbar; the hovered item gets the amber fill + dark text (the OPEN menu's hover idiom) while
the selected item shows amber text on a subtle lighter fill — hover wins when both. Keyboard
behavior must survive the restyle: focus draws an accent border (no default dashed rect), arrow
keys and type-ahead navigate, keyboard-open works, the inner toggle never steals focus; a
disabled combo dims to ~0.45 opacity. The pure-mode combos' `sys:Int32` tags and every
`SelectedValuePath` flow (`Tag`, `Game`) keep working unchanged, localization labels render as
today, and the per-site explicit sizes (26/28/30 px) stay untouched.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 723 green; visually verified by the user)

- [ ] All six combos render dark in closed state: panel-light fill, deep border, white text,
      secondary chevron
- [ ] Popup renders dark: panel-light background, deep border, thin dark scrollbar, no white
      flash on open
- [ ] Hovered popup item = amber fill + dark text; selected item = amber text on subtle lighter
      fill; hover wins when both; untouched items = white on panel
- [ ] Keyboard focus shows an accent border, not the dashed default focus rect
- [ ] Arrow keys, type-ahead and keyboard open/select work; the inner toggle is not focusable
- [ ] Disabled combo dims to ~0.45 opacity
- [ ] `sys:Int32` pure-mode tags and `SelectedValuePath` flows (`Tag`, `Game`) bind exactly as
      before
- [ ] `{loc:Loc ...}` and VM-provided labels render unchanged
- [ ] Per-site explicit sizes (26/28/30 px) unchanged
- [ ] Build green, full suite green
- [ ] Manual visual verification on all six sites

**Spec:** `.scratch/ui-custom-controls/spec.md`
