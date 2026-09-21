# 01: CFX facts plumbing

**What to build:** Server-published executable-build and pool-size facts reach the resolved CFX-validated server profile, so the connect flow can read them without knowing CFX var names. Specifically: `sv_defaultGameBuild`, `sv_replaceExeToSwitchBuilds`, and `sv_poolSizesIncrease` flow through the existing facts chain (CfxVars → CfxService response → ServerRequirements → ServerRequirementsResolver) as CFX-agnostic nullable fields, with invalid/missing values resolving to null.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] `sv_defaultGameBuild` (int), `sv_replaceExeToSwitchBuilds` (bool), `sv_poolSizesIncrease` (raw JSON string) are read from the CFX `/single/` response vars.
- [ ] `ServerRequirements` carries CFX-agnostic `DefaultBuild` (int?), `ReplaceExecutable` (bool?), `PoolSizesIncrease` (string?) mapped from those vars by `ServerRequirementsResolver`.
- [ ] Missing or invalid var values resolve to null (strict boolean parsing: only `"true"`/`"false"`).
- [ ] NO domain class names a CFX var; mapping stays in the service/facts layer per ADR 0001.
