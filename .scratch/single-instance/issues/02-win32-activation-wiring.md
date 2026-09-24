# 02: Win32 window activation + App wiring

**What to build:** the real `IExistingWindowActivator` using Win32 interop (`FindWindow` /
`ShowWindow(SW_RESTORE)` / `SetForegroundWindow` — matching the launcher window by title
"CFX Launcher by CodeRanger"), plus `App.OnStartup` wiring: the guard runs before the
object graph is built; when a second instance is detected the app activates the existing
window and calls `Shutdown()` without showing anything. No unit tests for the interop (OS
seam, per repo convention); verified manually by double-launching.

**Blocked by:** 01 (Single-instance decision logic).

**Status:** resolved

## Answer

`MutexSingleInstanceLock` (`Local\CFX Launcher.SingleInstance`, abandoned-safe) +
`Win32ExistingWindowActivator` (`FindWindow` by window title, `SW_RESTORE` +
`SetForegroundWindow`), wired first in `App.OnStartup`: duplicate instance exits silently via
`Shutdown()`, lock disposed on `Exit`. Verificado manualmente (double-launch + minimized
restore).

- [ ] Second launch of the exe focuses the existing window and exits
- [ ] Minimized existing window restores to normal and gets foreground
- [ ] No crash/dialog on the second instance
- [ ] Manual verification: double-click twice, minimized-restore case
- [ ] Build + suite green

**Spec:** `.scratch/single-instance/spec.md`
