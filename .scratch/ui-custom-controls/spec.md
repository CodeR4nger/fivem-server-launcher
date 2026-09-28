# Feature: Custom dark controls and animated menus

Status: ready-for-agent

## Problem Statement

The launcher's identity is a dark UI (charcoal panels, amber accent), but the WPF default
ComboBox and CheckBox render with the light Windows theme: white dropdowns, white checkboxes
clash hard against the panels. Dropdown menus also open abruptly with no motion, which makes
the app feel less fluid than the rest of the interface (which already animates the LOCAL DEV
panel slide).

## Solution

View-scoped custom templates for ComboBox and CheckBox that follow the existing token brushes
and typography, adopted by every dropdown and checkbox in the app, with a short slide+fade
animation when a dropdown popup opens — consistent with the LOCAL DEV panel's existing
250 ms ease-out motion language.

## User Stories

1. As a user, I want every dropdown to render dark (panel background, thin dark scrollbar,
   amber highlight for the selected/hovered item), so the UI stops flashing white.
2. As a user, I want checkboxes that render as dark boxes with the amber accent when checked,
   so they match the theme.
3. As a user, when I open any dropdown I want it to slide/fade in briefly, so interactions
   feel fluid.
4. As a user, I want keyboard and disabled states to keep working correctly (arrow keys, type
  ahead, focus visuals, disabled opacity), so restyling does not cost functionality.
5. As a user, I want the editable-free combos (settings client, language, game filter, pure
   mode, game, local dev fields) all to look and behave identically, so the app feels coherent.

## Implementation Decisions

- Styles live in MainView's existing view-scoped resources (no app-level theme dictionaries,
  no new files beyond shared converters if strictly needed).
- Templates use only the existing token brushes (`PanelBrush`, `PanelLightBrush`, `AccentBrush`,
  `AccentDarkBrush`, `TextBrush`, `SecondaryTextBrush`, `BorderBrush`, `BackgroundBrush`) and
  the existing thin dark ScrollBar style for popup lists.
- The popup animation is a XAML storyboard (translate-Y + opacity, ~200 ms, ease-out) triggered
  on popup open; no code-behind.
- Every ComboBox and CheckBox instance adopts the shared styles (settings preferred client,
  settings language, dev pure mode, dev build (when it becomes a combo in the game-build
  feature), local dev cfx/pure/game, browser game filter, all checkboxes: auto-launch, requires
  Steam/Discord, hide full/empty).
- Localization bindings (`{loc:Loc ...}` and VM labels) are unaffected; templates must keep
  binding-agnostic.
- sys:Int32 tag mechanics for pure-mode combos and the GameClientOption SelectedValuePath flow
  must keep working unchanged under the new templates.

## Testing Decisions

- Pure view glue: no unit tests (same stance as existing converters/styles). The full suite must
  stay green; the localizer and VM behaviors are already covered.
- Manual visual verification per the ui-design skill: dark rendering, animation, keyboard
  navigation, disabled states, on all five combo sites and all checkboxes, before committing.

## Out of Scope

- App-wide theming/resource dictionary refactor; restyling the window chrome; animations beyond
  the dropdown open (no window/panel transitions); custom TextBoxes/Buttons (already styled).

## Further Notes

- Runs after the security/performance tickets 05-09 per the agreed order (A B C D).
- Motion language precedent: the LOCAL DEV side panel slide (60 px + fade, 250 ms ease-out).
