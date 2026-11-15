# 01: Verify the staged asset against the published sha256 digest

**What to build:** The feed carries the asset's `digest` (`sha256:<hex>`) into `LauncherUpdate.Sha256`; a present-but-unusable digest is no update, an absent one is null. The applier hashes the staged file and rolls back (false, exe untouched, nothing left behind) on mismatch; a null digest falls back to the size check. Spec decision 1.

**Blocked by:** none

**Status:** resolved

## Answer

`LauncherUpdate` gained the optional trailing `Sha256` (lowercase hex). `GitHubReleaseFeed` binds the asset `digest` and parses `sha256:<64 hex>` (case-insensitive, normalized to lowercase); an absent digest yields `Sha256 = null`, a present-but-unusable one (other algorithm, wrong length, non-hex, empty) yields no update. `UpdateApplier.IsStagedAssetIntactAsync` keeps the size check and then, when a digest is carried, stream-hashes the staged file and refuses on mismatch through the existing rollback.
