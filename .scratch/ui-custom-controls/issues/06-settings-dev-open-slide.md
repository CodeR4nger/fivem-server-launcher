# 06: Settings + Dev Mode crossfades, LOCAL DEV panel slide — both directions

**What to build:** Opening and closing the app's panel surfaces animates both ways. Settings
and the Dev Mode areas crossfade (opacity-only, 250 ms — a vertical slide read awkwardly
against the layout, user-refined), and the normal-mode surfaces they replace (the status block
and the split button) fade inversely through the same mechanism, so the normal<->dev swap is a
true crossfade with both sides fading over the same slots. The dialog's LOCAL DEV side panel
keeps its 60 px slide in from the right, reversed on close. Because a plain flag-to-Visibility
binding collapses a panel before the first frame of the closing motion, Visibility is driven
through a MultiBinding (open flag OR the panel's own opacity > 0,
`Views/PanelVisibilityStateConverter` on a Self binding) — the fade doubles as the close-hold
state, and the collapse happens on its own when the fade completes. Hidden-by-flag panels rest
at opacity 0 with To-only motions (reopen mid-close resumes smoothly); the visible-by-default
surfaces use the converter's "invert" parameter, which also preserves their old
FallbackValue=Visible behavior on malformed inputs. Style storyboards cannot target a sibling
holder (MC4011), which is why the hold rides the panel itself. Pure XAML.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 739 green; visually verified by the user after two refinements: vertical slides replaced by pure crossfades, and the dev content restructured as a top-anchored RowSpan overlay after mounting it was found to resize the shared Auto rows — the outgoing status/split jumped to center mid-fade. The fade-as-hold visibility mechanism (`PanelVisibilityStateConverter`, Self-binding on the fading Opacity) replaces the originally planned instant closes; Style storyboards cannot target sibling holders per MC4011, verified in a harness)

- [ ] Settings panel fades in on open (250 ms) and fades out on close
- [ ] Dev controls and dev launch areas crossfade; the status block and split button fade
      inversely, so the normal<->dev swap crossfades both sides over the same slots
- [ ] The dialog's LOCAL DEV side panel slides in from the right and back out on close (also
      when the address stops being loopback mid-dialog)
- [ ] Panels collapse on their own exactly when the closing fade completes — never early,
      never stuck visible
- [ ] Reopening mid-close resumes smoothly (To-only motions, no snap)
- [ ] Visible-by-default surfaces stay visible at startup and on malformed inputs (old
      FallbackValue behavior preserved via "invert")
- [ ] The open-or-fading converter carries direct unit tests: flag passes through, fading
      keeps mounted, done collapses, invert flips the default-visible semantics, ctor pin
- [ ] Mutual exclusivity, value persistence and all bound behavior unchanged
- [ ] No new code-behind (pure XAML triggers)
- [ ] Build green, full suite green
- [ ] Manual visual verification: settings open/close, the normal<->dev crossfade both ways,
      the LOCAL DEV panel open/close, rapid close/reopen

**Spec:** `.scratch/ui-custom-controls/spec.md`
