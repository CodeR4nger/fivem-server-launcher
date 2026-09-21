# 02: Prepare a required app (start it if missing, wait until running)

**What to build:** The launcher can "prepare" a required external app: if it is already
running, preparation succeeds instantly without touching it; otherwise the launcher starts it
and waits (polling) until it is running, bounded by a timeout, so the subsequent connect is
not rejected for a missing app. Preparation never crashes: a failed start or a timeout yields
a clean failure the caller can turn into a status message.

**Blocked by:** 01 (Start a required external app)

**Status:** resolved

- [x] Preparing an app that is already running succeeds without any start attempt.
- [x] Preparing a missing app starts it and waits, succeeding as soon as readiness is detected.
- [x] Preparing an app that never becomes running fails cleanly after a bounded number of
      checks (deterministic: attempts × injected wait, no wall-clock dependency in tests).
- [x] A failing start (e.g. unregistered scheme) fails cleanly and never throws.
- [x] Reads readiness through the existing readiness seam and starts through the new starter
      seam; the wait between checks is an injectable hook so tests are instant.

## Resolution

- `Launch/ExternalAppPreparer(readiness, starter, Func<Task>? wait = null, int maxAttempts = 120)`:
  already-running → `true` (no start); else start (swallowed → `false`), then poll readiness
  up to `maxAttempts` × injected wait; default wait = `Task.Delay(500ms)`; per-attempt
  check; `false` on timeout/start failure; never throws to the caller.
- Tests: `ExternalAppPreparerTests` (4): already running, becomes-running-mid-poll (transition
  fake), never-running → bounded checks (5 = 1 initial + 4 attempts), starter throws → false.
  Test fakes: `FakeRequirementReadiness` (stateful `Func<bool>` + check counter) and
  `FakeExternalAppStarter` in `tests/.../Launch/FakeRequirementPreparerSeams.cs`. Suite 226 green.

## Comments

- Spec: `.scratch/requirement-preparation/spec.md`.
- Existing `FakeRequirementReadiness` covers the readiness seam; a small fake starter fake is
  added by this ticket's tests.