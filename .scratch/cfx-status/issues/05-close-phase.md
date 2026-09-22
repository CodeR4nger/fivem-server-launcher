# 05: Close phase + manual E2E

**Type:** task

**What to build:** full suite green, docs reconciled (AGENTS.md, roadmap phase status, spec
status `done`, ticket checkboxes), code review clean, commit. Manual E2E: three status rows
render with live values, dots/colors correct, dev-mode hide still works, no crash when CFX
statuspage is unreachable (keeps last known/`UNKNOWN`).

**Blocked by:** 04.

**Status:** resolved

- [x] Full suite green (387 tests, build clean, 0 errors besides pre-existing CS8619 — csproj build shows none besides that one).
- [x] Docs reconciled (AGENTS.md, roadmap `Status:` done, spec status `done`, ticket checkboxes).
- [x] Code review (Standards + Spec): no hard violations. Judgement calls accepted as repo-precedented
      (converter hardcodes palette-matching brushes like `GameClientToBrushConverter`; duplicated
      `SetProperty`/`Freeze` helpers mirror `SavedServerItem`/`MainViewModel`).
- [x] Manual E2E verified by the user (three rows render with live values, Dev Mode hides them).
- [x] Commit (conventional English).