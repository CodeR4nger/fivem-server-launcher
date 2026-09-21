# 04: Auto-launch on startup

**What to build:** when the user has auto-launch enabled and a last-used server exists, starting
the launcher immediately connects to that server through the exact same `ConnectAsync` flow
(including required-app preparation), without any address typing. With auto-launch off, or no
remembered server, startup behaviour is unchanged. An auto-launch failure surfaces the same status
messages a manual connect would — the window stays usable.

**Blocked by:** 03 (remember last server on successful connect).

**Status:** resolved

- [x] Auto-launch fills the box and runs the normal connect flow at startup when enabled with a
      remembered address; off/absent flags skip the connect entirely.
