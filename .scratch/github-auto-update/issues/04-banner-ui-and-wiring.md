# 04: Banner overlay UI + composition-root wiring

**What to build:** A small non-modal banner overlay in the main view, inheriting the custom
control styling: when a new version is available it shows the new version with a one-click
UPDATE button and a dismiss control; in-progress shows the update being applied; failed shows a
failed hint and stays dismissable. The composition root constructs the real release feed and
the real updater (with their real file/process seams), injects them into the view model, and
starts the check after the window is up. Every banner string is a localization key shipped in
English (source of truth) and every shipped language file — the audit test enforces
completeness.

**Blocked by:** 03 (the VM state the banner binds to).

**Status:** ready-for-agent

- [ ] The banner renders only in available / in-progress / failed states and is hidden
      otherwise; it is never modal and never blocks the saved-servers list or the connect flow
- [ ] UPDATE commands the view model's update command; dismiss commands dismiss — no business
      logic in code-behind
- [ ] In-progress and failed visuals follow the VM state
- [ ] Banner strings are localization keys present in `en.json` and every shipped language file;
      the localization audit is green
- [ ] Any XAML-instantiated type has a true parameterless constructor (the BAML rule)
- [ ] Composition-root wiring: real feed + updater injected; the check starts post-startup and
      never blocks
- [ ] Suite green; visual verification by the user

**Spec:** `.scratch/github-auto-update/spec.md`
