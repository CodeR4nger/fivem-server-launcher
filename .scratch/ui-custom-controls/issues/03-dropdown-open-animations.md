# 03: Dropdown open animations (combo popups + OPEN split menu)

**What to build:** Every dropdown in the app expands and fades in when it opens, unrolling out
of its origin, and collapses away again on close. Combo popups animate inside the dark
template from ticket 01 (~200 ms ease-out, fired from template triggers — no code-behind; the
popup needs `AllowsTransparency` so the motion renders cleanly). Because the popup window
hides the instant `IsDropDownOpen` flips false, the close renders inside a held-open popup:
the popup's `IsOpen` is driven through a MultiBinding (open OR close-hold,
`Views/DropdownOpenStateConverter`) on an invisible marker — opening snaps the hold on,
closing plays the reverse motion while a discrete keyframe releases the hold at 200 ms so
the popup hides exactly when the collapse ends; both motions are To-only so a reopen
mid-collapse resumes smoothly, and the popup content is not hit-testable while closing. The
OPEN split menu lives in the layout (not a popup), so both its motions animate the menu's
Height inside its show/hide code-behind glue: content clipped to the sweeping border, bottom
edge anchored — it unrolls upward out of the button seam and collapses back into it, the
lower area gliding along instead of jumping. Repeated open/close cycles, keyboard-open and
click-away close must stay glitch-free — no stuck transforms, no stuck-open popups.

**Blocked by:** 01 (the popup storyboard lives inside the dark ComboBox template).

**Status:** resolved (build + suite 729 green; visually verified by the user after three refinements: expand-from-origin instead of slide, layout-carried split-menu expand, fluid closes on both dropdown kinds. Two WPF traps were root-caused in a numeric harness before landing: an explicit Height clamps the child's measure constraint (MeasureCore), and a To-only DoubleAnimation throws when its origin is NaN)

- [ ] Combo popups expand+fade in ~200 ms ease-out on open, growing from the button seam, and
      collapse with the reverse motion on close; the popup hides exactly when the collapse
      ends (never early, never stuck open)
- [ ] Popup renders correctly during motion (`AllowsTransparency`; no clipping or black flash)
- [ ] Reopening mid-collapse resumes smoothly (To-only motions, no snap back to 0)
- [ ] The close-hold converter keeps the popup alive only while the collapse runs (direct unit
      tests: open passes through, holding keeps it alive, bad inputs close, ctor pin)
- [ ] OPEN split menu expands upward out of the button seam on open and collapses back into
      it on close — the Height-driven motions carry the lower area (no layout jump); closing
      from mid-expand reverses smoothly
- [ ] Rapid open/close cycles, keyboard-open (F4/Alt+Down) and click-away close stay
      glitch-free
- [ ] Keyboard interaction (arrows, type-ahead) unaffected while the popup animates
- [ ] Build green, full suite green
- [ ] Manual visual verification on both dropdown kinds, across the combo sites

**Spec:** `.scratch/ui-custom-controls/spec.md`
