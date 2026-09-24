# 01: Browser item mapping

**What to build:** a pure mapping from a catalog `Master.Server` entry to a browser row:
display name (`sv_projectName` var, fallback `hostname`, with CFX `^N` color codes stripped),
game (`gamename` var via `CfxVars`), players/max (`clients`/`svMaxclients`), the connect
endpoint (first `connectEndPoints`), and the canonical cfx id (`EndPoint`). Servers whose only
endpoint is the `https://private-placeholder.cfx.re/` sentinel map with their cfx-join form
(`cfx.re/join/<id>`) as the address. Entries without `Data` are skipped by the caller, not the
mapper.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

`ViewModels/ServerBrowserItem` (immutable, private ctor + `FromServer`): display name =
`sv_projectName` fallback `hostname`, `^[0-9r]` codes stripped; game via `CfxVars`; address =
first connect endpoint, cfx-join form for the `private-placeholder.cfx.re` sentinel or missing
endpoint. 5 tests green.

- [ ] `sv_projectName` is preferred over `hostname`
- [ ] `^N` color codes are stripped from the display name
- [ ] Game maps from `gamename` (null when unknown)
- [ ] Players/max come from `clients`/`svMaxclients`
- [ ] Hidden-sentinel servers use the cfx-join form as address
- [ ] CfxId is the catalog `EndPoint`
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/server-browser-view/spec.md`
