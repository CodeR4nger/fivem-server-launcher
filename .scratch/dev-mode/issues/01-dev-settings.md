# 01: Dev settings fields on LauncherSettings

**What to build:** `LauncherSettings` gains two nullable Dev Mode fields (`DevGameBuild`,
`DevPureMode`) that persist through the existing storage. Missing fields in an old file deserialize
as null. No behavior changes — purely the persisted model.

**Blocked by:** none.

**Status:** resolved

- [ ] `LauncherSettings.DevGameBuild` / `DevPureMode` (both `int?`, default null).
- [ ] Round-trip test and missing-field tolerance test green.
