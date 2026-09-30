# 05: Close phase — real-release E2E + docs

**What to build:** End-to-end proof against the real world, with releases published by hand
using the documented build+publish commands (the CI-workflow alternative was declined): publish
a real release, run an older build, see the banner, click UPDATE, and confirm the launcher
swapped itself and relaunched into the new version with its portable data intact. Also close
the feature: offline and no-release conditions stay silent, the suite is green, AGENTS.md's
current-state section describes the delivered updater, and the spec and tickets are resolved.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] A real hand-published release (tag `vX.Y.Z` + `CFXLauncher.exe` asset, built with the
      documented build+publish commands) triggers the banner on an older build
- [ ] One-click UPDATE swaps the running exe and relaunches into the new version; the portable
      `launcher-settings.json` / `saved-servers.json` beside the exe survive untouched
- [ ] Offline and no-release conditions are verified silent: the launcher starts normally with
      no banner and no error
- [ ] The relaunched instance is not rejected by the single-instance guard in the real run
- [ ] AGENTS.md current state updated (version source, feed seam, choreography, banner state,
      wiring)
- [ ] Spec and tickets resolved; suite green; commits follow the conventional style

**Spec:** `.scratch/github-auto-update/spec.md`
