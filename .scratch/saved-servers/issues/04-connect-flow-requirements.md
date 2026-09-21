# 04: Connect flow applies requirements + readiness

**What to build:** connecting carries the requirement story end to end. The connect flow takes the target (typed address or a selected saved server), applies the saved server's manual Steam/Discord flags into resolution (merged with published `sv_enforceSteamAuth`), checks readiness of the required apps, and surfaces the result in the status: required + running → "Steam ✓ / Discord ✓"; required + missing → "Requires Steam (not running)". Launching behaves exactly as today once the status is ready.

**Blocked by:** 02 (Effective Steam/Discord requirements), 03 (App requirement readiness detection)

**Status:** resolved

- [x] Connect uses the selected saved server's address and manual flags when present, typed address otherwise.
- [x] Status text reflects each required app's readiness (ready ✓ / not running) for servers that require it.
- [x] "Don't bother if everything is ready": nothing extra is shown when no requirements apply or all are running.
- [x] `MainViewModel` connect-flow tests with fake resolver, fake readiness, fake launcher.
- [x] Suite green (207).

## Comments
- "Selected saved server" is resolved by matching the typed address against the repository (`IServerRepository.FindByAddress`, added to the seam contract); the list-with-selection UI ships in ticket 05.
- Readiness status interpretation: the blocking missing state is surfaced ("Requires Steam (not running)", joined by " / " for several) and launch is skipped; when all required apps are running (or none are required) nothing extra is shown and launch proceeds as today — per the ticket's "don't bother" bullet, overriding the spec's earlier "Steam ✓" example.
- In `MainViewModel` tests the resolver is faked via the `CfxService` HTTP seam (`FakeHttpMessageHandler`), the established repo pattern; `IRequirementReadiness` and `IServerRepository` are faked directly.