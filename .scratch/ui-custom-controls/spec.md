# Feature: Custom dark controls and animated menus

Status: ready-for-agent

## Problem Statement

The launcher's identity is a dark UI (charcoal panels, amber accent), but the WPF default
ComboBox and CheckBox render with the light Windows theme: white dropdowns, white checkboxes
clash hard against the panels. Dropdowns also open abruptly with no motion — both the ComboBox
popups and the OPEN split button's hand-rolled menu — which makes the app feel less fluid than
the rest of the interface (which already animates the LOCAL DEV panel slide).

On top of that, the small glyph buttons (2x refresh, edit pencil, 2x move arrows) each carry a
copy-pasted inline ControlTemplate plus a per-instance TextBlock hover style — three near
identical patterns duplicated five times — so there is no single clear place to restyle or
adjust the idiom.

Finally, reordering saved servers — by the move arrows or by dragging — is a jump cut: the
affected rows teleport to their new slots, which feels stiff next to the rest of the motion
language.

And the app's surface transitions are jump cuts too: the settings panel, the Dev Mode swap and
the server browser overlay all appear and disappear instantly, so moving between the app's
major surfaces reads as abrupt.

## Solution

View-scoped custom templates for ComboBox and CheckBox that follow the existing token brushes
and typography, adopted by every dropdown and checkbox in the app; one shared IconButtonStyle
that all five glyph buttons adopt (hover/disabled live in one place); a short expand+fade
animation when any dropdown opens — the menu grows out of its origin (ComboBox popups and the
OPEN split menu alike) and collapses away again on close — consistent
with the LOCAL DEV panel's ease-out motion language; a short row slide whenever saved
servers are reordered, so both reorder paths land visibly where the row came from; and the
app's surface transitions join the same motion family — settings, Dev Mode and the LOCAL DEV
panel slide in and out, and the server browser pushes in and out.

## User Stories

1. As a user, I want every dropdown to render dark (panel background, thin dark scrollbar,
   amber highlight for the hovered item), so the UI stops flashing white.
2. As a user, I want checkboxes that render as dark boxes with an amber check when checked,
   so they match the theme.
3. As a user, when I open any dropdown — a combo popup or the OPEN split menu — I want it to
   expand into place out of its origin and collapse away again on close, so interactions feel
   fluid.
4. As a user, I want keyboard and disabled states to keep working correctly (arrow keys, type
   ahead, focus visuals, disabled opacity), so restyling does not cost functionality.
5. As a user, I want all the small glyph buttons (refresh, pencil, move arrows) to share one
   look, so hover and disabled feedback is identical everywhere.
6. As a user, I want the non-editable combos (settings client, settings language, dev pure
   mode, loopback dialog pure mode + game, browser game filter) to all look and behave
   identically, so the app feels coherent.
7. As a user, when I move a saved server — with the arrows or by dragging — I want the rows to
   slide into their new positions, so the reorder reads as motion instead of a jump cut.
8. As a user, when I open or close the settings panel, Dev Mode or the server browser, I want
   the surface to slide in and out (and the loopback dialog's local dev panel with it), so
   moving between the app's surfaces feels continuous.

## Implementation Decisions

- Styles live in MainView's existing view-scoped resources (no app-level theme dictionaries,
  no new files; no new converters are needed).
- Templates use only the existing token brushes (`PanelBrush`, `PanelLightBrush`, `AccentBrush`,
  `AccentDarkBrush`, `TextBrush`, `SecondaryTextBrush`, `BorderBrush`, `BackgroundBrush`) and
  the existing thin dark ScrollBar style for popup lists.
- ComboBox template (non-editable only — all six sites are; an editable template is YAGNI):
  closed state renders as PanelLightBrush fill, deep border, white text, `SecondaryTextBrush`
  chevron; keyboard focus shows an `AccentBrush` border (no default dashed focus rect);
  disabled dims to ~0.45 opacity (same as `BaseButtonStyle`). The inner ToggleButton is
  `Focusable=False` so keyboard focus, arrow keys and type-ahead stay on the ComboBox.
- Combo popup: `PanelLightBrush` background, deep border, thin dark scrollbar; popup items —
  hovered item gets `AccentBrush` fill + `BackgroundBrush` text (matches the OPEN menu's
  hover idiom), the selected item gets amber text on a subtle lighter fill (distinct from
  hover; hover wins when both), unselected/unhovered items are white-on-panel.
- CheckBox template (two-state; all five sites bind plain bools): dark box — `PanelLightBrush`
  fill + deep border; checked = amber check glyph only (the border stays deep — user-verified
  refinement); hover and keyboard focus brighten the border to the quiet grey; disabled dims to
  ~0.45 opacity.
- Popup animation is a XAML storyboard (top-anchored ScaleY 0->1 + opacity, ~200 ms, ease-out —
  the menu unrolls out of its origin rather than translating in; snappier than the panel's
  250 ms because the element is smaller, same motion language) triggered on popup open from
  template triggers; no code-behind. The popup needs `AllowsTransparency=True` so the
  transform/opacity render cleanly. The OPEN split menu lives in the layout (not a popup), so
  its expand animates the menu's Height in its show/hide code-behind glue — content clipped to
  the sweeping border, bottom edge anchored — so the menu unrolls upward out of the button
  seam and the lower area glides up with it instead of jumping (user refinement: the
  render-transform version teleported the layout first). The fade rides a pure XAML
  `IsVisible` style trigger.
- One shared `IconButtonStyle` (transparent template + centered glyph, glyph in
  `SecondaryTextBrush` flipping to `AccentBrush` on hover, one normalized disabled treatment
  ~0.35 opacity) replaces the five inline copies. The refresh buttons' per-site
  border-colored disabled trick and the arrows' per-site opacity triggers are folded into that
  single disabled treatment — a cooling-down refresh stays visible-but-dim instead of
  near-invisible.
- Reorder animation: FLIP-style row slide — after the collection move lays rows out at their
  final slots, the affected containers (the moved row plus every row it displaced) render a
  `TranslateTransform` from their pre-move offset and animate it to zero (~200 ms, ease-out,
  same motion language). WPF has no layout-transition primitive, so this is pure visual glue
  in the existing reorder code-behind; both paths (arrow buttons and drag drop) funnel through
  `SavedServers.Move`, and rows are fixed-height, so the deltas derive from the move indices
  and the measured row height — no pre-move capture, no VM changes. The reorder semantics
  (persist-first, bounds safety, filter rejection) are untouched; a rejected/no-op move never
  animates.
- Reorder animation edge behavior: rapid successive moves must stay coherent (a previous slide
  completes instantly or retargets — no stacked or stuck transforms); a scroll during a slide
  cancels it; on drag drop the row un-dims and the drop line hides before the slide (the
  existing `EndDragVisuals` order); the move-arrows overlay and drop line always target the
  final layout — they never ride the animation.
- Surface transitions (settings, Dev Mode, the dialog's LOCAL DEV side panel): both directions.
  Settings and the Dev Mode areas are pure crossfades (opacity-only, 250 ms — the vertical
  slide read awkwardly against the layout; user-refined), and the normal-mode surfaces they
  replace (the status block and the split button) fade inversely, so the normal<->dev swap is
  a true crossfade: both sides fade over the same slots. The dev content is a single
  top-anchored overlay spanning the lower rows (never a participant in their sizing), so
  mounting it cannot resize the rows the fading normal-mode surfaces live in — otherwise the
  outgoing status/split get repositioned mid-fade. The LOCAL DEV side panel keeps its 60 px
  slide in from the right, reversed on close. Visibility is never bound to the flag directly
  — it is driven through a MultiBinding (open flag OR the panel's own opacity > 0,
  `Views/PanelVisibilityStateConverter` on a Self binding): the panel's fade doubles as its
  close-hold state, so the closing motion renders inside the still-mounted panel and the
  collapse happens on its own when the fade completes (Style storyboards cannot target a
  sibling holder — MC4011 — which is why the hold rides the panel itself). Hidden-by-flag
  panels rest at opacity 0 with To-only motions so a reopen mid-close resumes smoothly; the
  visible-by-default surfaces (status, split) rest at opacity 1 and use the converter's
  "invert" parameter, which also keeps them visible on malformed inputs exactly like their
  old FallbackValue. Panels are not hit-testable once their flag drops.
- Server browser transition: push-in from the right on open (~80 px + fade, 250 ms ease-out)
  and the reverse on close — the one surface that animates both ways. The VM flag stays the
  single source of truth for browser state; the close is a small code-behind visual-glue step
  that plays the out-storyboard, collapses the overlay on completion, and cancels (re-shows
  instantly) if the browser is reopened mid-close. Deliberately NOT the always-mounted
  opacity-0 pattern: an invisible-but-visible overlay leaks keyboard focus and hit-testing
  into hidden content; delaying only the collapse keeps `Visibility` semantics intact.
- Adoption sites — combos: settings preferred client, settings language, dev pure mode,
  loopback dialog pure mode, loopback dialog game, browser game filter (the future game-build
  combo from the game-build-dropdown feature will adopt the same style); checkboxes: auto-launch,
  requires Steam/Discord, hide full/empty; glyph buttons: saved-list refresh, browser refresh,
  edit pencil, move up/down. Per-site explicit sizes (26/28/30 px heights) stay as-is — the
  layout is user-tuned; the shared styles own colors, typography and interaction states only.
- Localization bindings (`{loc:Loc ...}` and VM labels) are unaffected; templates stay
  binding-agnostic.
- `sys:Int32` tag mechanics for pure-mode combos and the `SelectedValuePath` flows
  (`Tag`, `Game`) must keep working unchanged under the new templates.

## Testing Decisions

- Pure view glue: the dropdown close state machine ships one shared converter
  (`Views/DropdownOpenStateConverter` — popup held open while the collapse renders) with
  direct unit tests (open passes through, holding keeps the window alive, bad inputs close,
  parameterless-ctor pin); everything else stays the same stance as existing converters and
  styles: no unit tests for view glue. The automated gate is the build (XAML/BAML markup
  compile) plus the full suite staying green; the localizer and VM behaviors are already
  covered.
- Manual visual verification per the ui-design skill before committing: dark rendering,
  animation, keyboard navigation, type-ahead, focus and disabled states — on all six combo
  sites, all five checkboxes, all five glyph buttons, and both dropdown kinds (combo popup +
  OPEN split menu). Reorder slides: arrow moves up/down, drag across several slots, rapid
  repeated moves, scrolling mid-slide. Surface transitions: settings open, Dev Mode open,
  the settings<->dev swap, browser open + close, and the rapid close/reopen cancel path.

## Out of Scope

- App-wide theming/resource dictionary refactor; restyling the window chrome; custom
  TextBoxes/Buttons (already styled); the game-build combo's content (phase 16 — it merely
  adopts these styles); editable combos; window-level transitions (open/close/minimize).
  Every in-app surface now animates both directions — dropdowns, panels and the browser.
- Drag-reorder niceties beyond the landing slide: no live "gap opens" relayout while hovering a
  slot, no animated scroll-into-view, no row add/remove animations (only reorder moves slide).

## Further Notes

- Roadmap phase 15 (v1.3), after reorder-saved-servers (done) — the move arrows it introduced
  are among the glyph buttons consolidated here, and the reorder slide rides its code-behind
  glue (drag measuring, arrows overlay).
- Motion language precedent: the LOCAL DEV side panel slide (60 px + fade, 250 ms ease-out).
- Motion family after this feature: dropdowns ~200 ms, reorder row slides ~200 ms, settings/
  dev/browser surface transitions ~250 ms — all short translate-or-scale + opacity, ease-out.
- The normalized glyph-button disabled state is a small deliberate visual change: refresh during
  its cooldown window goes from near-invisible (`#080808` glyph) to dimmed-but-visible.
