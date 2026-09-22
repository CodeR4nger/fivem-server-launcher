# 03: Close phase + final Release build

**Type:** task

**What to build:** full suite green after 01–02, final standards+spec code review,
final Release build (`dotnet build FiveMServerLauncher.slnx -c Release`) with 0
errors, docs reconciled (roadmap phase status, spec `done`, ticket checkboxes),
conventional English commit, roadmap notes the produced Release artifacts.

**Blocked by:** 02.

**Status:** resolved

- [x] Full suite green (422 = 387 baseline + 35 new: CfxStatusItemTests 13, converter tests 22)
- [x] Final code review (Standards + Spec) on the closing diff — no hard violations; Spec: Accept
- [x] Final Release build 0 errors; artifacts documented in the roadmap
- [x] Docs reconciled (roadmap/spec/tickets)
- [x] Commit (conventional English)