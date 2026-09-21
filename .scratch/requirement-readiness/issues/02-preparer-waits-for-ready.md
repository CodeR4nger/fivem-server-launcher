Status: resolved
Type: task
Blocked by: 01

# 02: preparer waits for ready, not running

`Launch/ExternalAppPreparer.TryPrepareAsync` becomes:

1. `readiness.IsReadyAsync(app)` -> `true`, touch nothing.
2. `readiness.IsRunningAsync(app)` false -> `starter.StartAsync(app)`.
3. Poll `IsReadyAsync(app)` every wait until ready -> `true`; give up `false` after `maxAttempts`.
4. Body wrapped in try/catch -> never throws.

Critical new behaviour: an app that is **already running but not ready** (Steam at the login
screen) is waited on **without restarting** it — this is the reported bug (
"is not waiting for steam to be fully loaded").

RED tests first, adapting `StatefulRequirementReadiness` to a ready axis:
- already ready -> true, nothing started
- not running -> started, becomes ready -> true
- running but not ready -> becomes ready -> true AND never started (the hole)
- never ready -> false after bounded attempts
- start throws / readiness throws / wait throws -> false, never throws

Done when `dotnet test` green.