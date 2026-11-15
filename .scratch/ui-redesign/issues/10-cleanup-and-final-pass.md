# 10: Cleanup and final pass

**What to build:** the redesign finished and documented. Remove styles, converters,
code-behind, view-model members and localization keys that nothing renders any more (the
localization audit and the build confirm). Update `AGENTS.md` (UI bullets describe the shell,
pages, theme, motion switch and layout tests; stale descriptions of the old overlays, kebab and
banner removed) and the README (new screenshots if it has any, the Inter license note). Do a
full manual pass and record it here: minimum size and maximized; English, Spanish, Russian and
Japanese; keyboard-only navigation of every page and dialog; animations on, Minimal, and with
Windows animations off; tray minimize/restore; a `cfxlauncher://` link and a duplicate launch
restoring the window; the published single-file exe (Inter renders, size before/after noted).

See `../spec.md`.

**Blocked by:** 06, 07, 08, 09.

**Status:** ready-for-agent

- [ ] No orphaned styles, converters, members or keys remain (list what was removed).
- [ ] `AGENTS.md` and README match the delivered UI.
- [ ] Manual pass results recorded with screenshots; any issue found is fixed or filed as a new ticket.
- [ ] Published exe checked; suite green on several full runs; zero warnings; CI green.
