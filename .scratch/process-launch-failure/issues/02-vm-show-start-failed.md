# 02: VM shows "No se puede lanzar" on StartFailed

**What to build:** `MainViewModel.ConnectAsync` switch gains a case for `LaunchResult.StartFailed` + tests.

**Blocked by:** 01

**Status:** resolved

- [x] Test: `StartFailed` → status text "No se puede lanzar", IsBusy=false, 0 process launches.
- [x] Suite green (110).

## Comments
