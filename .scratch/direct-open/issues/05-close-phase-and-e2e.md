# 05: Close phase and manual E2E

**What to build:** close the direct-open phase: full suite green, docs reconciled (AGENTS.md and
DOC.md updated to describe the open action and the data-bound dropdown), tickets resolved. Manual
E2E on a real machine: opening Legacy FiveM from the dropdown boots it cleanly (the launch-shell
context lesson does not apply to a direct exe start, but verify), opening FiveM Enhanced boots it,
and the dropdown offers only the installed clients.

**Blocked by:** 04 (data-bound OPEN control).

**Status:** in-progress (code complete; manual E2E pending)

- [x] Full suite green; docs (AGENTS.md, DOC.md) reconciled with the implemented behavior.
- [ ] Manual E2E: Legacy FiveM opens cleanly from the OPEN button.
- [ ] Manual E2E: FiveM Enhanced opens from the OPEN button (and from an Enhanced-server connect).
- [ ] Manual E2E: the dropdown lists only installed clients.
- [x] Code-review (Standards + Spec) clean; conventional English commit.