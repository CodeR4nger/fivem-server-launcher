Status: resolved
Type: spec

# UI connect flow: ViewModel + binding del flujo "Entrar al servidor"

## Problem Statement

La app ya sabe resolver una dirección (`ServerResolver`) a un `ServerProfile` y orquestar su lanzamiento (`GameLauncher` + seam `IGameProcessLauncher`), pero no hay capa de UI que lo use: la MainView actual es un mock estático (sin TextBox de dirección, sin comando, sin binding). DOC.md define que la UI lógica viva fuera del code-behind (MVVM) y que el launcher arranque desde una dirección ingresada.

Esta fase introduce la capa `ViewModels` con un `MainViewModel` testeable con fakes de procesos/HTTP/DNS, y los bindings XAML mínimos para disparar el flujo. **Fuera de alcance: proceso real de lanzamiento, PathResolver, splash/progress UI, guardar estado entre ejecuciones.**

## Solution

Nueva capa `ViewModels` (pequeña, una pieza):

- `MainViewModel` (INotifyPropertyChanged manual, sin librerías):
  - `ServerAddress` (string, two-way bound al TextBox).
  - `IsBusy` (bool; mientras hay una llamada en vuelo).
  - `StatusText` / `StatusColor` (muestra "Listo" / "Resolviendo..." / "Lanzando..." / "Abriendo FiveM Enhanced"/ "error").
  - `ConnectCommand` (`ICommand` puro en C#: `AsyncRelayCommand` manual, sin librerías).
  - ctor toma `ServerResolver`, `GameLauncher`.
- La decisión Enhanced vs Legacy/RedM ya la devuelve `LaunchResult` — la UI solo lo muestra (nunca lanza proceso de Enhanced aquí; el launcher real se agregará fuera de esta fase).
- Implementación XAML: TextBox arriba, botón "Entrar al servidor" con `Command="{Binding ConnectCommand}"`, y un `TextBlock` simple de estado. El dropdown de selección de cliente y la ventana/estilos existentes se mantienen (no se cambian en esta fase).

## User Stories

1. Como jugador, quiero ingresar `cfx.re/join/y4lg95` y hacer click en "Entrar al servidor" para que el launcher resuelva y empiece a conectar.
2. Como jugador, quiero que se muestre "Dirección inválida" si escribo algo malformado.
3. Como jugador, quiero que se muestre "Abriendo FiveM Enhanced" si el servidor requiere Enhanced (no lanza proceso desde esta fase).
4. Como desarrollador, quiero testear todo esto sin tocar la red, procesos ni filesystem.

## Implementation Decisions

- `MainViewModel.ConnectAsync`: `IsBusy=true` → `StatusText="Resolviendo..."` → try/catch `InvalidAddressException` → error → finally `IsBusy=false`.
  - De la resolución, llama a `GameLauncher.ConnectAsync(profile)` y siguiente `LaunchResult`:
    - `Connect(uri)` → `StatusText="Lanzando..."` / "Success"; el URI ya fue delegado al seam (proceso real se implementa en fase futura).
    - `OpenClient(client)` → `StatusText="Abriendo <client>"`; nunca se lanza proceso desde esta fase (el launcher real será con otro seam más tarde).
- Invariante: si `IsBusy=true`, `ConnectCommand.CanExecute=false` (para evitar doble click).
- TDD en el seam: tests con `FakeDnsResolver`, `FakeHttpMessageHandler`, `FakeGameProcessLauncher` (ya existen), juntando un `ServerResolver` con la fachada `GameLauncher` mockeada.
- El XAML se modifica manualmente (fuera del RED/GREEN) una vez verificada la lógica; el binding se revisa inspeccionando el XAML.
- `AsyncRelayCommand` (~20 líneas) es decisión propia; no hay lógica de control de flujo extra.

## Testing Decisions

- Tests del `MainViewModel` con fakes: sin `App` ni `Window`.
- Múltiples formas de probar el comando: `vm.ConnectCommand.Execute(null)` + esperar a que `IsBusy` vuelva a false.
- Reglas de AGENTS.md: no usar filesystem ni procesos reales; los fixtures existentes (`FakeHttpMessageHandler`, `FakeDnsResolver`) cubren esto.
- Tests unitarios del VM; la validación del XAML se hace manualmente (inspección).

## Out of Scope

- El proceso real de lanzamiento del cliente (seam `IGameProcessLauncher` real aún sin implementación; la UI está lista para que eso llegue sin cambio).
- PathResolver / detección de instalación de FiveM.
- Splash / progress UI elaborada.
- Persistir la última dirección entre ejecuciones.
- Steam/Discord, timestamps, queries a Core de configuraciones.
- Comando de OpenClient que de verdad abra el cliente.

## Further Notes

- Recordatorio DOC.md: si hay algo que el usuario necesite ver, status update textual; sin splash ni pantalla de carga en esta fase.
- Cerrar fase: actualizar AGENTS.md con la capa `ViewModels`, actualizar conteo de tests, commit convencional en inglés.