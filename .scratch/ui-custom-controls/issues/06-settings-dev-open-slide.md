# 06: Settings + Dev Mode open slide

**What to build:** Opening the settings panel or Dev Mode slides the content in instead of
popping it: the settings panel slides down from behind the settings/dev button row and the dev
controls + dev launch areas slide down into their slots (~40 px + fade, 250 ms ease-out), via
the same DataTrigger/BeginStoryboard pattern the LOCAL DEV dialog panel already uses. Closes
stay instant (the `Visibility` collapse is immediate, matching that precedent), so the
settings<->dev mutual-exclusivity swap never runs concurrent exit animations — the swap reads
as the closing surface vanishing while the opening surface slides in. No new code-behind.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Settings panel slides down + fades in on open (~40 px, 250 ms ease-out)
- [ ] Dev controls and dev launch areas slide in the same way on open
- [ ] Closes are instant; the settings<->dev swap never animates two exits at once
- [ ] Mutual exclusivity, value persistence and all bound behavior unchanged
- [ ] No new code-behind (pure XAML triggers)
- [ ] Build green, full suite green
- [ ] Manual visual verification: settings open, dev open, the swap between them

**Spec:** `.scratch/ui-custom-controls/spec.md`
