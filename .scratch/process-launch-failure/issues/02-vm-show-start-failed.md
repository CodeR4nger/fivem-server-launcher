# 02: VM muestra "No se puede lanzar" ante StartFailed

**What to build:** `MainViewModel.ConnectAsync` switch gana un caso para `LaunchResult.StartFailed` + tests.

**Blocked by:** 01

**Status:** resolved

- [x] Test: `StartFailed` → status text "No se puede lanzar", IsBusy=false, 0 process launch.
- [x] Suite verde (110).

## Comments