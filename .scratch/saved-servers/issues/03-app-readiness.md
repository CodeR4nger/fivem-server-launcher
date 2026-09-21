# 03: App requirement readiness detection

**What to build:** the launcher can tell whether a required external app (Steam, Discord) is running, so it can warn before connecting. An `ExternalApp` shared enum (Steam, Discord), a readiness seam `IRequirementReadiness.IsRunningAsync(ExternalApp)`, and a real `ProcessReadinessChecker` with an injectable process-name check (default: real process lookup). Not-found or check failure → not running, never throws. The seam is named for a future sharpening from "running" to "ready"/"authenticated".

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `ExternalApp` enum with `Steam` and `Discord`.
- [x] `ProcessReadinessChecker` uses the injected `Func<string,bool>` process-name seam (default real process lookup), returns true when present.
- [x] Missing process / throw check → false, exception never propagates.
- [x] Tested with fake process checks (no real processes).
- [x] Suite green (198).