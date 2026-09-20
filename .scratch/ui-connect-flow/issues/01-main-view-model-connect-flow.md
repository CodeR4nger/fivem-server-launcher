# 01: MainViewModel lanza flujo de conexión (happy path)

**What to build:** primera slice vertical. `MainViewModel` con `ServerAddress`, `IsBusy`, `StatusText`, `ConnectCommand` (`AsyncRelayCommand`). Al ejecutar el comando con una dirección válida (cfx.re/join), resuelve, GameLauncher lanza (fake) y el VM vuelve a Ready con "Lanzando FiveM...".

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `MainViewModel` en `ViewModels/` con `ServerAddress`, `.IsBusy`, `StatusText`, `ConnectCommand`.
- [x] `AsyncRelayCommand` (helper `ICommand` manual, sin librerías) con `CanExecute` ligado a `IsBusy`.
- [x] `ConnectAsync` happy path: `IsBusy` true durante, luego resolver + launcher (fake) y `StatusText` final correcto.
- [x] Tests usando `ServerResolver` real con fakes HTTP/DNS y `GameLauncher` con `FakeGameProcessLauncher`.
- [x] Suite completa verde.

## Comments