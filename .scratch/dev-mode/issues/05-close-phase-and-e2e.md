# 05: Close phase and manual E2E

**What to build:** close the Dev Mode phase: full suite green, docs reconciled (AGENTS.md + DOC.md
describe Dev Mode and its persistence boundary), tickets resolved. Manual E2E: dev launch with
`-b<build>`/`-pure_<n>` opens Legacy with the flags; `-cl2` opens a second client; connect-with-
server ignores dev overrides; build/pure persist across restarts; second client does not persist.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] Full suite green; docs reconciled.
- [ ] Manual E2E: dev launch with flags boots Legacy FiveM through the shell and shows the flags.
- [ ] Manual E2E: build/pure persist across restarts; `-cl2` not persisted.
- [ ] Manual E2E: connecting to a server ignores dev overrides.
- [ ] Code-review clean; conventional commit.
