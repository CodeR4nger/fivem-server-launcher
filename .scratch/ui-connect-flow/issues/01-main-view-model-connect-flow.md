# 01: MainViewModel launches the connect flow (happy path)

**What to build:** first vertical slice. `MainViewModel` with `ServerAddress`, `IsBusy`, `StatusText`, `ConnectCommand` (`AsyncRelayCommand`). When the command runs with a valid address (cfx.re/join), it resolves, GameLauncher launches (fake) and the VM returns to Ready with "Lanzando FiveM...".

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `MainViewModel` in `ViewModels/` with `ServerAddress`, `.IsBusy`, `StatusText`, `ConnectCommand`.
- [x] `AsyncRelayCommand` (manual `ICommand` helper, no libraries) with `CanExecute` tied to `IsBusy`.
- [x] `ConnectAsync` happy path: `IsBusy` true during, then resolve + launcher (fake) and correct final `StatusText`.
- [x] Tests using real `ServerResolver` with HTTP/DNS fakes and `GameLauncher` with `FakeGameProcessLauncher`.
- [x] Full suite green.

## Comments
