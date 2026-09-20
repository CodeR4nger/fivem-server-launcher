Status: resolved
Type: spec

# Composition root: grafo real (@App) + launcher real de procesos

## Problem Statement

El flujo de conexión está completo salvo por la composición real: `App.xaml` arranca `MainWindow` directamente (`StartupUri`), pero nadie construye `MainViewModel` con `ServerResolver`, `GameLauncher`, etc. Tampoco hay `DnsResolver` ni `GameProcessLauncher` reales (solo los interfaces/fakes). No podemos testear esto con procesos reales (regla del repo), así que la parte real queda como código mínimo y seam-based.

## Solution

- `Service/DnsResolver.cs`: implementación real de `IDnsResolver` con `System.Net.Dns` (`GetHostAddressesAsync`). Sin tests unitarios (llamada OS real) — decisión documentada; el seam `IDnsResolver` ya está testeado.
- `Launch/GameProcessLauncher.cs`: implementación real `IGameProcessLauncher` que hace `Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true })`. Sin tests de proceso real.
- `App.xaml.cs`: composition root. En `OnStartup` construímos el grafo: `HttpClient` → CfxService/ServerCatalog → ServerResolver → GameLauncher → `MainViewModel` → asignado como `DataContext` de `MainWindow`. Quitar `StartupUri` de `App.xaml` y hacer Show explícito.
- `MainWindow`/`MainView` se conecta vía DataContext ahora (el binding ya existe).

## User Stories

1. Como jugador, quiero abrir el launcher, escribir `cfx.re/join/y4lg95` y poder entrar a servidores en CFX.
2. Como desarrollador, quiero el arranque del grafo al inicio visible en App.

## Implementation Decisions

- `DnsResolver.ResolveToIpAsync(string host)` → `GetHostAddressesAsync(...)`, primer IPv4, `host:port` se construye en upstream.
- `GameProcessLauncher.StartAsync(Uri)`: `Process.Start` con `UseShellExecute=true` para delegar al protocolo registrado.
- `App.xaml.cs`: com minimal wiring sin lógica; `MainWindow.Ctor()` puede tomar `MainViewModel` como parámetro o asignar `DataContext` en `OnStartup`.
- Sin tests de proceso real — el seam/README ya lo cubre.
- Sin DI container — composición manual.

## Testing Decisions

- El código real de proceso no se testea.
- Suite verde sin tests nuevos (es una capa de composición).
- Verificación manual del app (no automatizada).

## Out of Scope

- PathResolver/detector de FiveM.
- Splash/progress; Steam/Discord; lifecycle; ventanas extras; pruebas de proceso manual.