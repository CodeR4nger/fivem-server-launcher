# 05: Reset pool sizes on default-pool servers

**What to build:** When a server runs default pool sizes (no `sv_poolSizesIncrease` published, or blank), priming resets `[Game] PoolSizesIncrease` to an empty value so a previously primed increase from another server doesn't linger and trigger restart-on-connect.

**Blocked by:** 03 (CitizenFX.ini preparer decision).

**Status:** done

- [x] `CitizenFxPreparer.BuildValues` always primes `PoolSizesIncrease` for CFX-validated Legacy FiveM profiles: the published JSON verbatim when non-blank, else an empty value.
- [x] Writer handles empty target values (replace a non-empty value with `key=`; no-op when already empty).
- [x] Gate/no-write invariants for unvalidated/Enhanced/RedM/not-installed unchanged; dead `values.Count == 0` fast path removed.
- [x] Spec, AGENTS.md and DOC.md updated; full suite green.