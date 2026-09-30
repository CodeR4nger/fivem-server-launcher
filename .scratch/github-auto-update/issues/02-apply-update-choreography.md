# 02: Apply an update (download → staged swap → relaunch)

**What to build:** Given a newer release (ticket 01's answer), one operation installs it with no
elevation, fully portable: download the tagged asset to a staged temp file, verify sanity
(expected asset name, non-trivial size), then swap on restart — rename the running exe (allowed
on Windows even while running), move the staged file into its place, relaunch the new exe, and
exit the old one. Every file/shell step sits behind the existing seam pattern (file + process
starter seams) so the whole choreography is verified with fakes and no real files or network in
tests. Any failure at any step — download, sanity check, rename, move, relaunch — leaves the
running exe untouched and reports failure: the user is never left with a broken or missing
launcher. The relaunched instance must not be eaten by the single-instance guard of the
still-exiting old one (the guard is released or bypassed as part of the handoff).

**Blocked by:** 01 (the release info shape: version + asset name/size/URL).

**Status:** ready-for-agent

- [ ] The success choreography runs in order — download → stage → sanity-check → rename the
      running exe → move the new exe in → relaunch → old exits — each step observable through
      the fakes
- [ ] The staged asset is sanity-checked (expected name, sane size) before any swap happens
- [ ] A download failure, sanity failure, or failing swap step performs no partial swap: the
      current exe keeps running and the failure is reported (no broken install, no deleted exe)
- [ ] The relaunch targets the new exe's final path through the process seam
- [ ] The single-instance handoff is safe: the relaunched instance is not treated as a duplicate
      of the exiting one
- [ ] No real network or filesystem in tests — fakes only
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/github-auto-update/spec.md`
