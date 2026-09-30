# 03: Banner state + UPDATE command (view model)

**What to build:** Shortly after the window is up — never blocking startup — the main view model
runs the release check and, when a newer version exists, exposes banner state the UI can bind
to: available (carrying the new version label), update-in-progress, failed, and dismissed. One
UPDATE command drives the install choreography end to end: available → in-progress, and on
success the app hands off to the relaunched new exe with no further user step; on failure the
banner shows failed and stays dismissable. A dismiss command hides the banner for the session —
updates are never forced, and the check simply runs again on the next launch. A failed check
(offline, rate-limited, GitHub down) changes nothing: no banner, no error, the launcher behaves
exactly as it did before this feature.

**Blocked by:** 01 (the feed), 02 (the choreography the UPDATE command drives).

**Status:** ready-for-agent

- [ ] The startup check runs after startup without blocking it; a silent or throwing check never
      breaks startup
- [ ] A newer release puts the banner in "available" with the new version visible to the UI
- [ ] UPDATE: available → in-progress; success ends in the relaunch handoff (no further user
      step); failure → failed state
- [ ] While in progress, re-triggering UPDATE is impossible (command gated)
- [ ] Dismiss hides the banner for the session; nothing else changes
- [ ] A feed outage or check failure produces no banner and no error
- [ ] Banner labels resolve through the localization seam (emitted at query time, re-emitted on
      language change — the VM-owned-label precedent)
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/github-auto-update/spec.md`
