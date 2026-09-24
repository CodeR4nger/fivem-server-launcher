# 01: Single-instance decision logic

**What to build:** a guard consulted at startup before anything else: try to acquire the
product-named mutex (`CFXLauncher.SingleInstance`). If the mutex is free, startup proceeds
normally. If it is already held by another instance, the guard activates the existing
launcher window (restore + bring to front, via an `IExistingWindowActivator` seam) and startup
aborts silently (the second process exits without building the window). Both the mutex
acquisition and the window activation are seams so unit tests never touch the OS.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

`Service/SingleInstanceGuard.TryStart()` with seams `ISingleInstanceLock` and
`IExistingWindowActivator`: mutex free → allow startup without touching windows; held →
best-effort activation (exceptions swallowed) then abort. 3 tests green.

- [ ] Mutex free → startup proceeds, no activation attempted
- [ ] Mutex held → existing window activated (restore + foreground) and startup aborts
- [ ] Activation failure still aborts silently (never crashes, no error dialog)
- [ ] Mutex released with the process (named mutex semantics, abandoned-safe)
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/single-instance/spec.md`
