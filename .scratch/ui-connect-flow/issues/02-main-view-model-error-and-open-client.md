# 02: MainViewModel shows error on invalid address and opens client for Enhanced

**What to build:** two behaviors:
1. `ConnectAsync` with an invalid address (e.g. whitespace, unknown) catches `InvalidAddressException` and shows `StatusText="Dirección inválida"` without launching anything; `IsBusy` returns to false.
2. `ConnectAsync` when `GameLauncher` returns `OpenClient(GameClient)` (Enhanced server) shows `StatusText="Abriendo ..."` without launching a process.

**Blocked by:** 01

**Status:** resolved

- [x] Test: invalid address → `StatusText="Dirección inválida"`, `IsBusy=false`, fake process with no calls.
- [x] Test: Enhanced `OpenClient` → `StatusText="Abriendo FiveMEnhanced..."`, fake process with no calls.
- [x] `IsBusy=false` even if `ConnectAsync` throws (`finally`).
- [x] Full suite green (109).

## Comments
