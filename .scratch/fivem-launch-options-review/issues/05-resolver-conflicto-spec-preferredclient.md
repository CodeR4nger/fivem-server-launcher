# 05: Resolve the spec conflict over PreferredClient

**What to build:** the `FiveMLaunchOptions` spec contradicts itself: it says "when opening directly, from `LauncherSettings.PreferredClient`" but also that "`LauncherSettings` is untouched". It is documented that the `GameClient` when opening directly is decided by the caller (e.g. the UI, reading `LauncherSettings` at its level) and that this phase introduces no factory that reads `LauncherSettings`. Doc-only: the spec is adjusted and, if applicable, DOC.md, with no code changes.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] The `.scratch/fivem-launch-options/spec.md` spec no longer contradicts itself about `PreferredClient` (it is clear who decides the `GameClient` when opening directly)
- [x] No code is introduced that reads `LauncherSettings`
- [x] The full suite stays green
