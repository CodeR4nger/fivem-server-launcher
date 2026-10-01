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

**Status:** resolved

## Answer

Delivered as VM-owned banner state in `MainViewModel`, wired through two optional nullable ctor
seams (`IReleaseFeed?` + `IUpdateApplier?`; both-or-none — unwired means the check no-ops, so
the ~50 pre-existing test constructions stayed untouched and the composition root owns the
real wiring in ticket 04). Surface: `IsUpdateAvailable` / `IsUpdateInProgress` /
`IsUpdateFailed` (mutually exclusive by construction, WPF-idiomatic bools the XAML can bind
through BooleanToVisibilityConverter), the computed `UpdateBannerText`
(`ILocalizer.Format("UpdateBannerText", tag)` — "New version: {0}", re-emitted on
`LanguageChanged` alongside the other VM-owned labels), `UpdateCommand`
(`ApplyUpdateAsync`, gated `IsUpdateAvailable && !IsUpdateInProgress`) and
`DismissUpdateCommand` (session-only hide; nothing re-shows the banner because the check runs
once).

Flow: `CheckForUpdateAsync` (private, swallowed whole — a crashing or offline check never
breaks startup and never shows a banner) runs at the END of `InitializeAsync`, after the
auto-launch connect, so a slow GitHub answer can never delay the user's game. UPDATE:
available → in-progress → the applier's `false` or a throw → failed (dismissable); `true`
means the applier already relaunched and exited the app, so the state deliberately stays
in-progress — no further user step. A mid-flight re-trigger is impossible: the command is
gated and a direct call no-ops (pinned with a gated fake).

The single new localization key (`UpdateBannerText`) shipped complete in all 11 language
files at once (the audit fails the build otherwise) — 10 translations drafted alongside
English, community-improvable like the rest.

Review follow-ups applied: mid-flight dismiss is now REFUSED (the dismiss command and method
gate on `!IsUpdateInProgress` — dismissing during the handoff would let a later applier
failure re-show a banner the user believes dismissed, breaking the session-dismiss promise;
pinned by a gated-fake test), and the check moved outside `InitializeAsync`'s startup
`try`/`catch` — it is fully self-swallowing, so a failed startup (IOException) still gets the
banner offering the update that fixes the broken version.

Tests (`MainViewModelTests`, 12 new): newer-release banner with composed text; no-update
silence; throwing feed survives startup; unwired behaves as before; hand-off passes the
announced update to the applier and stays in-progress; applier failure/throw → failed
banner; mid-flight no-reapply; dismiss from available and from failed; mid-flight dismiss
refused; language change re-emits the banner text.

- [x] The startup check runs after startup without blocking it; a silent or throwing check never
      breaks startup
- [x] A newer release puts the banner in "available" with the new version visible to the UI
- [x] UPDATE: available → in-progress; success ends in the relaunch handoff (no further user
      step); failure → failed state
- [x] While in progress, re-triggering UPDATE is impossible (command gated)
- [x] Dismiss hides the banner for the session; nothing else changes
- [x] A feed outage or check failure produces no banner and no error
- [x] Banner labels resolve through the localization seam (emitted at query time, re-emitted on
      language change — the VM-owned-label precedent)
- [x] Suite green (RED → GREEN → REFACTOR) — 811 tests green

**Spec:** `.scratch/github-auto-update/spec.md`
