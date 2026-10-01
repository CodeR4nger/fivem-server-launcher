# 05: Close phase — real-release E2E + docs

**What to build:** End-to-end proof against the real world, with releases published by hand
using the documented build+publish commands (the CI-workflow alternative was declined): publish
a real release, run an older build, see the banner, click UPDATE, and confirm the launcher
swapped itself and relaunched into the new version with its portable data intact. Also close
the feature: offline and no-release conditions stay silent, the suite is green, AGENTS.md's
current-state section describes the delivered updater, and the spec and tickets are resolved.

**Blocked by:** 04.

**Status:** resolved

## Answer

Closed with a real-world run. The release `v1.3.0` was published by hand (the documented
build+publish commands; `CFXLauncher.exe`, 62.3 MB, attached under tag `v1.3.0` — the
CI-workflow alternative was declined at ticketing). A simulated-old build (a temporary,
never-committed `AppInfo.Version` = 1.2.9 published as a single-file exe in the temp
workspace) verified the whole chain live:

- Pre-publish startup with NO releases existing: normal start, no banner, no error.
- After publishing: the banner appears seconds after the window is up.
- One click on UPDATE: "Updating...", the download, the swap, and the launcher relaunches
  itself into the new version — the relaunched instance was NOT rejected by the
  single-instance guard (the applier releases it before relaunching), portable data beside the
  exe survived, and the replaced exe remains as a `CFXLauncher.exe.old-<guid>` backup.

Docs closed out in the same commit: AGENTS.md (current state, composition-root graph, the
auto-update architecture bullet, test fakes), roadmap phase 17 marked done, changelog entry
under 1.3.0, spec status done. Suite 819 green across the feature; commits 59a6fa3..7a3770d
plus this close-out.

- [x] A real hand-published release (tag `vX.Y.Z` + `CFXLauncher.exe` asset, built with the
      documented build+publish commands) triggers the banner on an older build
- [x] One-click UPDATE swaps the running exe and relaunches into the new version; the portable
      `launcher-settings.json` / `saved-servers.json` beside the exe survive untouched
- [x] Offline and no-release conditions are verified silent: the launcher starts normally with
      no banner and no error
- [x] The relaunched instance is not rejected by the single-instance guard in the real run
- [x] AGENTS.md current state updated (version source, feed seam, choreography, banner state,
      wiring)
- [x] Spec and tickets resolved; suite green; commits follow the conventional style

**Spec:** `.scratch/github-auto-update/spec.md`
