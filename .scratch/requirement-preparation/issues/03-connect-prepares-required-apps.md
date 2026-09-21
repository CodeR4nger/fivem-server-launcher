# 03: Connect flow prepares missing required apps before launching

**What to build:** When connecting to a server whose effective requirements include an app
that is not running, the launcher prepares it instead of blocking: the status shows
"Starting Steam..." / "Starting Discord...", the app is started and the launcher waits until
it is running, then the connect proceeds. If an app cannot be started or never becomes
running, the connect is aborted with "Could not start Steam" / "Could not start Discord" and
nothing is launched. When nothing is required or everything is already running, the flow
behaves exactly as today (no extra noise). The real starter and preparer are wired into the
composition root.

**Blocked by:** 02 (Prepare a required app)

**Status:** resolved

- [x] Connect with a missing required app shows "Starting {app}...", starts the app, waits for
      readiness, then launches as today.
- [x] Connect with an app that never becomes running (or fails to start) shows
      "Could not start {app}" and does not launch.
- [x] Both Steam and Discord required and missing are both prepared before launching.
- [x] Connect with everything ready shows no new status and launches as today (v1 messaging
      "Requires X (not running)" is exercised only as the pre-preparation fallback and is
      otherwise gone along the connect flow).
- [x] Composition root wires the real starter + preparer into the view model; the whole suite
      stays green.
- [ ] Manual checks on a real machine: Steam-required server with no Steam running → Steam
      opens and the game launches after it appears; Discord required but not installed →
      connect aborts with "Could not start Discord".

## Resolution

- `MainViewModel` ctor gains `ExternalAppPreparer` (5th dep); the old hard-block branch is
  replaced by a sequential prepare loop per missing app: `StatusText = "Starting {app}..."`,
  `TryPrepareAsync` → false → `"Could not start {app}"` and no launch. Dead
  `FormatMissingRequirements` removed. `FindMissingRequirementsAsync` (via
  `IRequirementReadiness`) still computes the missing list so "don't bother if everything is
  ready" holds.
- `App.xaml.cs` composition root: builds `ProcessReadinessChecker` once, passes it plus
  `ExternalAppPreparer(readiness, new ExternalAppStarter(new ProcessStarter(),
  new UriSchemeRegistration()))` to the view model.
- Review-driven fixes applied: `ExternalAppPreparer` now checks readiness *immediately after*
  the start and waits between checks (spec "as soon as it is running"), and wraps the whole
  body so readiness/start/wait exceptions all degrade to `false` (categorical never-throws);
  new preparer tests for throwing readiness/wait; new VM test asserting the transient
  "Starting Steam..." status is shown while waiting.
- Tests: `ExternalAppPreparerTests` 6, `MainViewModelTests` connection-flow rewired to the
  prepare contract (per-app stateful readiness fake). Suite 231 green.
- Pending: manual E2E (Steam opens; Discord-missing gives "Could not start Discord").

## Comments

- Spec: `.scratch/requirement-preparation/spec.md`.
- Replaces the hard "block" branch introduced by the saved-servers phase connect flow.