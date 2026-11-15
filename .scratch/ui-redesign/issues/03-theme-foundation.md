# 03: Theme foundation

**What to build:** the new visual language applied to the existing screen, with the layout
unchanged. Inter is bundled (license text in the repo, README mention) and set at the window
root with Segoe UI as fallback. The theme dictionary gains the token set (ground, panel,
surfaces, borders, three text levels, accent, game tag and status colours, radii, spacing,
type ramp) and the reusable control styles: primary and secondary pill buttons, icon button,
filter chip (optional dot and count), segmented control, toggle switch (a styled
`ToggleButton`), filled game tag, server mark (icon with initials fallback and a deterministic
colour from the name), card, text input, dropdown, section header, settings row, modal frame,
popup menu, empty state. The Interface animations setting exists end to end in the model
(`Default`/`Minimal`, persisted, Windows' animation setting honoured) together with the shared
motion resources and the single switch that turns them all off; its UI arrives in ticket 09.

See `../spec.md` (Theme, Reusable controls/styles, Interface animations).

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] Inter renders in the running app (Debug and published single-file); license file present.
- [ ] Every token has one definition; text/background pairs used by the styles reach 4.5:1 (listed in this ticket).
- [ ] Game tags use exactly `#F5A623` / `#4DA3FF` / `#E05252` with near-black bold text, filled, small radius.
- [ ] Server mark initials/colour logic is tested; every XAML-instantiated type passes the parameterless-ctor reflection test.
- [ ] Interface animations: setting persisted, defaults to Default, effective motion = Default AND Windows animations on; tested at the view-model seam; with motion off, no storyboard runs.
- [ ] The current screen uses the new styles without layout changes; screenshots recorded; existing layout test still passes; zero warnings.
