# 01: Verify the staged asset against the published sha256 digest

**What to build:** The feed carries the asset's `digest` (`sha256:<hex>`) into `LauncherUpdate.Sha256`; a present-but-unusable digest is no update, an absent one is null. The applier hashes the staged file and rolls back (false, exe untouched, nothing left behind) on mismatch; a null digest falls back to the size check. Spec decision 1.

**Blocked by:** none

**Status:** open
