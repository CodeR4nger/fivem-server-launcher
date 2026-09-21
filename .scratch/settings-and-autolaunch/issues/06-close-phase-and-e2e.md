# 06: Close phase and manual E2E

**What to build:** close the settings phase: full suite green, docs reconciled (AGENTS.md and
DOC.md updated to describe the new settings flow, the `LastServerAddress` field, and the
auto-launch behaviour), tickets resolved. Manual E2E on a real machine: change the preferred client
and the auto-launch toggle in the settings panel and verify persistence across restarts; connect
once to a server, close and relaunch to confirm the box is pre-filled and the launcher connects
automatically.

**Blocked by:** 05 (settings UI panel).

**Status:** in-progress

- [ ] Full suite green; docs reconciled with the implemented behaviour.
- [ ] Manual E2E: preferred-client + auto-launch changes persist across restarts.
- [ ] Manual E2E: last server address is remembered and auto-connect triggers on next launch.
- [ ] Code-review clean; conventional commit(s) closing the phase.
