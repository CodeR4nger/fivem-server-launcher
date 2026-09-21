Status: resolved
Type: task
Blocked by:

# 01: ready probe on the readiness seam

`IRequirementReadiness` gains `Task<bool> IsReadyAsync(ExternalApp app)` next to `IsRunningAsync`.
Real `Service/ProcessReadinessChecker` implements it:

- Steam: `steam` process AND `steamwebhelper` process (both through the existing
  `Func<string,bool>` seam) AND registry `ActiveUser != 0` (new injectable `Func<int?>` seam,
  default real registry read, same pattern as `ClientInstallLocator`).
- Discord: equal to `IsRunningAsync(Discord)`.
- Unknown/none: false; any throwing/absent probe -> false, never throws.

Fakes upgrade: `FakeRequirementReadiness` (Running + default ready; ready counters) and
`StatefulRequirementReadiness` (dual predicates; `ready` defaults to `running`).

Tests (RED first): Steam ready only when all three signals hold; helper absent -> not ready;
`ActiveUser == 0` -> not ready; Steam process absent -> not ready; Discord ready == running;
unknown -> false; throwing registry -> false; `IsRunningAsync` unchanged and still returns the
coarse process result.

Done when `dotnet test` green and the seam vocabulary matches the ADR (no new middle men).