# 01: Open action at the Application seam

**What to build:** a real "open this client" action at the existing orchestrator. The launcher asks
for a game client and it gets opened: the installed executable is resolved, started through the
process seam, and the outcome is reported as a launch result (`OpenClient` when launched,
`NotInstalled` when no executable is found, `StartFailed` on a launch exception). This is the
primitive the dropdown and the Enhanced connect flow both call.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] `OpenAsync(GameClient)` returns `OpenClient` and starts the resolved executable through the
      process seam when the client is installed (Legacy and Enhanced).
- [x] `OpenAsync` returns `NotInstalled` and does not touch the process seam when the executable
      cannot be resolved.
- [x] `OpenAsync` returns `StartFailed` when the start throws.
- [x] The result contract gains `NotInstalled(GameClient)`; `OpenClient` means "launched".
- [x] The composition root passes the install locator to the launcher so the open action can
      resolve executables.