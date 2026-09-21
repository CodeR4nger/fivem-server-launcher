# 03: CitizenFX.ini preparer decision

**What to build:** The single place that decides what to write before connecting: only CFX-validated Legacy FiveM profiles that publish at least one relevant fact. `DefaultBuild` comes from `sv_defaultGameBuild` when published, else from `sv_enforceGameBuild` when `sv_replaceExeToSwitchBuilds` is `true`; `PoolSizesIncrease` mirrors the server's pool-size JSON verbatim. The write is best-effort — install missing, empty facts, or any IO failure resolve to a no-op that never throws.

**Blocked by:** 01 (CFX facts plumbing), 02 (CitizenFX.ini config writer).

**Status:** done

- [ ] `ICitizenFxPreparer` seam with a real preparer deciding target `[Game]` values from a profile.
- [ ] Gated to CFX-validated Legacy FiveM profiles; Enhanced/RedM/unvalidated/no-install/no-facts → no write.
- [ ] Precedence: `DefaultBuild` ← `sv_defaultGameBuild`; else `sv_replaceExeToSwitchBuilds == true` && `sv_enforceGameBuild` present → `DefaultBuild` ← that build; `PoolSizesIncrease` passed through verbatim when non-empty.
- [ ] Best-effort: locator/writer failures are swallowed; the preparer never throws.
- [ ] Decision covers "only when the file's current value differs" via the writer's no-touch contract; preparer itself does not read the ini.
