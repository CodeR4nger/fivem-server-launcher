# 03: Dropdown open animations (combo popups + OPEN split menu)

**What to build:** Every dropdown in the app slides and fades in briefly when it opens. Combo
popups animate inside the dark template from ticket 01 (translate-Y + opacity, ~200 ms
ease-out, fired from template triggers — no code-behind; the popup needs `AllowsTransparency`
so the motion renders cleanly). The OPEN split menu gets the same motion via a style trigger on
its visibility flip (the LOCAL DEV panel's DataTrigger/BeginStoryboard pattern) with its
structure and show/hide code-behind untouched. Repeated open/close cycles, keyboard-open and
click-away close must stay glitch-free — no stuck transforms, no re-open flash.

**Blocked by:** 01 (the popup storyboard lives inside the dark ComboBox template).

**Status:** ready-for-agent

- [ ] Combo popups slide+fade in ~200 ms ease-out on open; closes stay instant
- [ ] Popup renders correctly during motion (`AllowsTransparency`; no clipping or black flash)
- [ ] OPEN split menu animates the same slide+fade on open; its structure and show/hide
      code-behind are unchanged
- [ ] Rapid open/close cycles, keyboard-open (F4/Alt+Down) and click-away close stay
      glitch-free
- [ ] Keyboard interaction (arrows, type-ahead) unaffected while the popup animates
- [ ] Build green, full suite green
- [ ] Manual visual verification on both dropdown kinds, across the combo sites

**Spec:** `.scratch/ui-custom-controls/spec.md`
