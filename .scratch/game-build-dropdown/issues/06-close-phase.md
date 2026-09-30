# 06: Close the game-build-dropdown phase

**What to build:** The two-axis review pass the phase is owed, plus the documentation that makes
the feature discoverable to the next session. Review the phase diff (fixed point: the last commit
before ticket 02 lands — ticket 01 is research and produces no code) against the repo's
standards and against the spec on both axes, fix what the review finds, then record the feature
in the places that describe the current state: the repo's current-state notes (including the
ticketing-time decisions: Dev Mode's list follows the CLIENT toggle; the dialog's GAME = None
disables the combo; no "Latest" marker on the newest build; build data auto-updates once per
session from CFX sources with curated-baseline fallback), the roadmap (tick off the
game-build-dropdown line, keep the remaining v1.3 item listed), and the changelog.

**Blocked by:** 01, 02, 03, 04, 05.

**Status:** ready-for-agent

- [ ] Two-axis review (repo standards + spec) over the phase diff, both axes reported separately
- [ ] Findings triaged: accepted findings fixed in-session, deferrals recorded with reasons
- [ ] Suite green after any fixes
- [ ] `AGENTS.md` current state describes the build dropdowns, the option engine + baseline,
      the session fetch service, and the recorded decisions
- [ ] The roadmap marks the feature (v1.3 phase 16) delivered; the remaining v1.3 item stays
      listed
- [ ] The changelog entry is user-visible phrased under the existing `1.3.0 (unreleased)`
      section

**Spec:** `.scratch/game-build-dropdown/spec.md`
