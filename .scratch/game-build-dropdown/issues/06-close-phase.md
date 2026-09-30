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

**Status:** resolved

## Answer

Both axes ran as parallel sub-agents over `git diff fa1908a...HEAD` (the five `feat(game-build)`
commits, b3d069a..a36c162).

**Standards axis:** no hard documented-standard violations — XAML changes are pure bindings,
tests follow the naming/Given-When-Then conventions and stay at seams, the outage catch set is
the sanctioned narrow pair, localization re-emission wired. Judgement-call findings and triage:

- **Fixed — `int.Parse` in `ParseBundleNames`** (also the spec axis's worst finding): a
  >10-digit `case` number would throw `OverflowException` through the narrow catches, leaking
  the seam's never-throws contract (only the app-root catch-all rescued it). Fixed with
  `int.TryParse` (skip absurd entries) + a regression test
  (`FetchAsync_WhenBundleCaseNumberOverflows_ShouldSkipItWithoutThrowing`; its first
  `Assert.Single` draft was wrong — the service *merges* over the 18-entry baseline — so the
  test pins count-preserving skip instead).
- **Fixed — Repeated Switches**: the `FiveM or RedM` gate appeared in both the engine's
  early-return and the dialog's `IsDialogGameBuildEnabled`, a lockstep risk the day a
  build-capable game joins (a `ny` section already exists upstream). Extracted
  `GameBuilds.SupportsBuildSelection(game)` as the single gate; pure refactor, suite green.
- **Deferred with reasons**: the duplicated catch pair in `GameBuildDataService` (the two
  catch sets deliberately express different policies — null vs baseline-names — a shared
  helper would muddy them); `IGameBuildDataService` having one implementation (mirrors the
  established `ICfxStatusService` sibling precedent); the 4-param `Options(game, storedBuild,
  noneLabel, data)` at two call sites (a bundle type for two call sites is over-abstraction);
  the pinning test restating the baseline (the repo's "snapshot is the proof" precedent);
  minor test-comment drift (merged `// Given / When` matches existing house style).

**Spec axis:** zero missing requirements, zero scope creep (the `[n] <name>` label change is
the recorded ticket-02 amendment, not creep). Findings:

- The `int.Parse` leak (same fix as above) — the only "looks wrong".
- The named-if-known raw entry vs the spec's "raw number entry" wording: a recorded decision
  (ticket 02 Answer, shown to and accepted by the user during the dry run) — now folded into
  the spec text so the plan of record matches the shipped behavior.
- Ticket 03's criterion wording "numbers come out newest-first" overstated the service seam
  (the engine sorts; the service passes file order through) — satisfied end-to-end per the
  order-agnostic engine design; noted, resolved ticket text left as its own historical record.

All recorded amendments confirmed honored: numbers replace/names merge (03), Dev Mode follows
the CLIENT toggle (04), GAME = None disables (05), `[n] <name>` labels (02).

**Docs:** AGENTS.md updated on all three fronts (the Dev Mode surface description, the v1.3
delivered bullet with the engine/service/decisions summary + remaining spec list corrected to
just `github-auto-update`, and a new Architecture bullet for the engine + dataset + session
service); roadmap phase 16 ticked with the close record; changelog entry added under
`1.3.0 (unreleased)` (user-visible phrasing); spec raw-entry line amended.

- [x] Two-axis review (repo standards + spec) over the phase diff, both axes reported separately
- [x] Findings triaged: accepted findings fixed in-session, deferrals recorded with reasons
- [x] Suite green after any fixes — 767 green (766 + the overflow regression test)
- [x] `AGENTS.md` current state describes the build dropdowns, the option engine + baseline,
      the session fetch service, and the recorded decisions
- [x] The roadmap marks the feature (v1.3 phase 16) delivered; the remaining v1.3 item stays
      listed
- [x] The changelog entry is user-visible phrased under the existing `1.3.0 (unreleased)`
      section

**Spec:** `.scratch/game-build-dropdown/spec.md`
