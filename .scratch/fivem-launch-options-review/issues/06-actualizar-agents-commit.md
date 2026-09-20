# 06: Update AGENTS.md and commit the phase

**What to build:** `AGENTS.md` reflects the real state after the `FiveMLaunchOptions` phase (single seam in Domain, `ToUri`/`ToCommandLineArgs` serializations, FiveM Enhanced restriction, suite with the new test count) and the phase is committed to the current branch with English conventional commits. It also validates that the acceptance criteria of tickets 02-03 in `.scratch/fivem-launch-options/` and of this phase are marked as `[x]`.

**Blocked by:** 01, 02, 03, 04, 05

**Status:** resolved

- [x] `AGENTS.md` describes `FiveMLaunchOptions` (seam, serializations, Enhanced in `Current state`/`Architecture`)
- [x] `AGENTS.md` reflects the real number of green tests (60)
- [x] The phase tickets have their `[x]` boxes verified against the code
- [x] The phase is committed with English conventional commit message(s) (`feat(domain)` 6f57c0a, `docs(agents)` 959f4d2)
- [x] The full suite stays green at the committed point
