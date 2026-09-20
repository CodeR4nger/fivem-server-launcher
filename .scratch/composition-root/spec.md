Status: resolved
Type: spec

# Composition root: real graph (@App) + real process launcher

## Problem Statement

The connect flow is complete except for the real composition: `App.xaml` starts `MainWindow` directly (`StartupUri`), but nobody builds `MainViewModel` with `ServerResolver`, `GameLauncher`, etc. There are also no real `DnsResolver` or `GameProcessLauncher` (only the interfaces/fakes). We cannot test this with real processes (repo rule), so the real part stays as minimal seam-based code.

## Solution

- `Service/DnsResolver.cs`: real implementation of `IDnsResolver` with `System.Net.Dns` (`GetHostAddressesAsync`). No unit tests (real OS call) — documented decision; the `IDnsResolver` seam is already tested.
- `Launch/GameProcessLauncher.cs`: real `IGameProcessLauncher` implementation that does `Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true })`. No real-process tests.
- `App.xaml.cs`: composition root. In `OnStartup` we build the graph: `HttpClient` → CfxService/ServerCatalog → ServerResolver → GameLauncher → `MainViewModel` → assigned as `DataContext` of `MainWindow`. Remove `StartupUri` from `App.xaml` and do an explicit Show.
- `MainWindow`/`MainView` is now connected via DataContext (the binding already exists).

## User Stories

1. As a player, I want to open the launcher, type `cfx.re/join/y4lg95` and be able to join servers on CFX.
2. As a developer, I want graph startup visible in App from the beginning.

## Implementation Decisions

- `DnsResolver.ResolveToIpAsync(string host)` → `GetHostAddressesAsync(...)`, first IPv4; `host:port` is built upstream.
- `GameProcessLauncher.StartAsync(Uri)`: `Process.Start` with `UseShellExecute=true` to delegate to the registered protocol.
- `App.xaml.cs`: minimal wiring with no logic; `MainWindow.Ctor()` may take `MainViewModel` as a parameter or assign `DataContext` in `OnStartup`.
- No real-process tests — the seam/README already covers it.
- No DI container — manual composition.

## Testing Decisions

- Real process code is not tested.
- Suite green with no new tests (this is a composition layer).
- Manual app verification (not automated).

## Out of Scope

- PathResolver/FiveM detector.
- Splash/progress; Steam/Discord; lifecycle; extra windows; manual process tests.
