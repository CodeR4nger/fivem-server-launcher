Status: resolved
Type: spec

# UI connect flow: ViewModel + binding for the "Entrar al servidor" flow

## Problem Statement

The app already knows how to resolve an address (`ServerResolver`) to a `ServerProfile` and orchestrate its launch (`GameLauncher` + `IGameProcessLauncher` seam), but there is no UI layer that uses it: the current MainView is a static mock (no address TextBox, no command, no binding). DOC.md defines that UI logic lives outside the code-behind (MVVM) and that the launcher starts from an entered address.

This phase introduces the `ViewModels` layer with a `MainViewModel` testable with process/HTTP/DNS fakes, and the minimal XAML bindings to trigger the flow. **Out of scope: real launch process, PathResolver, splash/progress UI, persisting state across runs.**

## Solution

New `ViewModels` layer (small, one piece):

- `MainViewModel` (manual INotifyPropertyChanged, no libraries):
  - `ServerAddress` (string, two-way bound to the TextBox).
  - `IsBusy` (bool; while a call is in flight).
  - `StatusText` / `StatusColor` (shows "Listo" / "Resolviendo..." / "Lanzando..." / "Abriendo FiveM Enhanced" / "error").
  - `ConnectCommand` (pure C# `ICommand`: manual `AsyncRelayCommand`, no libraries).
  - ctor takes `ServerResolver`, `GameLauncher`.
- The Enhanced vs Legacy/RedM decision is already returned by `LaunchResult` — the UI only displays it (never launches an Enhanced process here; the real launcher will be added outside this phase).
- XAML implementation: TextBox on top, "Entrar al servidor" button with `Command="{Binding ConnectCommand}"`, and a simple status `TextBlock`. The client-selection dropdown and the existing window/styles are kept (unchanged in this phase).

## User Stories

1. As a player, I want to enter `cfx.re/join/y4lg95` and click "Entrar al servidor" so the launcher resolves and starts connecting.
2. As a player, I want "Dirección inválida" shown if I type something malformed.
3. As a player, I want "Abriendo FiveM Enhanced" shown if the server requires Enhanced (no process is launched in this phase).
4. As a developer, I want to test all of this without touching the network, processes or filesystem.

## Implementation Decisions

- `MainViewModel.ConnectAsync`: `IsBusy=true` → `StatusText="Resolviendo..."` → try/catch `InvalidAddressException` → error → finally `IsBusy=false`.
  - From the resolution, it calls `GameLauncher.ConnectAsync(profile)` and then on `LaunchResult`:
    - `Connect(uri)` → `StatusText="Lanzando..."` / "Success"; the URI was already delegated to the seam (real process is implemented in a future phase).
    - `OpenClient(client)` → `StatusText="Abriendo <client>"`; no process is ever launched in this phase (the real launcher will come with another seam later).
- Invariant: if `IsBusy=true`, `ConnectCommand.CanExecute=false` (to prevent double-click).
- TDD at the seam: tests with `FakeDnsResolver`, `FakeHttpMessageHandler`, `FakeGameProcessLauncher` (already exist), joining a `ServerResolver` with the mocked `GameLauncher` facade.
- The XAML is modified manually (outside RED/GREEN) once the logic is verified; the binding is checked by inspecting the XAML.
- `AsyncRelayCommand` (~20 lines) is our own decision; no extra control-flow logic.

## Testing Decisions

- `MainViewModel` tests with fakes: no `App` or `Window`.
- Multiple ways to exercise the command: `vm.ConnectCommand.Execute(null)` + wait for `IsBusy` to return to false.
- AGENTS.md rules: no filesystem or real processes; existing fixtures (`FakeHttpMessageHandler`, `FakeDnsResolver`) cover this.
- VM unit tests; XAML validation is done manually (inspection).

## Out of Scope

- The real client launch process (real `IGameProcessLauncher` seam still unimplemented; the UI is ready for that to arrive without change).
- PathResolver / FiveM install detection.
- Elaborate splash / progress UI.
- Persisting the last address across runs.
- Steam/Discord, timestamps, Core configuration queries.
- An OpenClient command that actually opens the client.

## Further Notes

- DOC.md reminder: if there is something the user needs to see, textual status update; no splash or loading screen in this phase.
- Phase close: update AGENTS.md with the `ViewModels` layer, update the test count, English conventional commit.
