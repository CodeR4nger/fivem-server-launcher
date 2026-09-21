# 05: Close phase and manual E2E

**What to build:** close the direct-open phase: full suite green, docs reconciled (AGENTS.md and
DOC.md updated to describe the open action and the data-bound dropdown), tickets resolved. Manual
E2E on a real machine: opening Legacy FiveM from the dropdown boots it cleanly, opening FiveM Enhanced boots it
(direct exe start also needs shell context — both go through `explorer.exe`), and the dropdown offers only the installed clients.

**Blocked by:** 04 (data-bound OPEN control).

**Status:** resolved

**Resolution notes:**
- E2E first pass: the OPEN button works, but FiveM Enhanced refused a direct `CreateProcess` start
  ("Rockstar Games Launcher could not be opened") — same shell-context lesson as the `fivem://` URI
  route. `StartExecutableAsync` now hands `explorer.exe "<path>"` to the shell, mirroring the URI
  path (test `StartExecutableAsync_ShouldHandExeToWindowsShell`).
- E2E retest: FiveM Enhanced opens cleanly (shell-route verified); the dropdown lists and opens the
  installed clients; Legacy FiveM goes through the same explorer route.

- [x] Full suite green; docs (AGENTS.md, DOC.md) reconciled with the implemented behavior.
- [x] Manual E2E: Legacy FiveM opens cleanly from the OPEN button.
- [x] Manual E2E: FiveM Enhanced opens from the OPEN button (and from an Enhanced-server connect).
- [x] Manual E2E: the dropdown lists only installed clients.
- [x] Code-review (Standards + Spec) clean; conventional English commit.