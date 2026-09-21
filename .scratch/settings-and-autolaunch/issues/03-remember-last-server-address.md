# 03: Remember last server on successful connect

**What to build:** every time the launcher successfully launches a server connection (i.e. real
launch of a `fivem://connect/...` URI or a client open after connect), the launcher persists the
typed server address as `LastServerAddress`. A launch that fails (`StartFailed`), cannot resolve
(`InvalidAddress`), or opens nothing (`NotInstalled`) leaves the remembered address untouched, so a
bad or aborted attempt never displaces the last *working* server.

**Blocked by:** 02 (VM reads settings: preferred-client seeding + save-on-change).

**Status:** resolved

- [x] `ConnectAsync` persists the trimmed `ServerAddress` as `LastServerAddress` on `Connect` or
      `OpenClient`.
- [x] `StartFailed` / `NotInstalled` / `InvalidAddress` leave the remembered address untouched.
