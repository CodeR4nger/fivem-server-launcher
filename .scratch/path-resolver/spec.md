Status: resolved
Type: spec

# PathResolver: detectar instalaciones de FiveM por ruta real

## Problem Statement

Hoy `GameLauncher.ConnectAsync` puede devolver `OpenClient(GameClient)` o `StartFailed`, pero no hay manera de distinguir "no pude conectar" de "el cliente no está instalado". DOC.md menciona `PathResolver` como una responsabilidad de Infrastructure; no hay seam ni implementación.

La ruta de instalación real (invesitgada/documentada):
- **Legacy (FiveM)**: `%localappdata%\FiveM\FiveM.app\FiveM.exe` (docs officiales de FiveM).
- **Enhanced**: `%localappdata%\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe` — exacto path confirmado por el usuario (se instala en esa carpeta por default).

## Solution

- `Service/IClientInstallLocator` (seam): dos métodos inyectables, `bool IsInstalled(GameClient)` y `string? GetExecutablePath(GameClient)`. Nunca null sobre IsInstalled; null solo para executable.
- `Service/ClientInstallLocator` (real): `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` + `Path.Combine(path base).\\FiveM.app\\FiveM.exe` (Legacy) o `path base\\FiveM for GTAV Enhanced\\FiveM.app\\FiveM.exe` (Enhanced).
- Sin tests reales de filesystem (se usa seam fake tests del repo).

## User Stories

1. Como jugador, si no está instalado Enhanced, el launcher me dirá que no hay escalating.
2. Como desarrollador, quiero testear detección sin tocar filesystem (seam fake).

## Implementation Decisions

- `IClientInstallLocator`: `Task<bool> IsInstalled(GameClient)` y `Task<string?> GetExecutablePath(GameClient)` — async-ready (symmetry con seams de Service).
- `ClientInstallLocator` real usa `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "FiveM.exe")` para Legacy, y como `Path.Combine(..., "FiveM for GTAV Enhanced", "FiveM.app", "FiveM.exe")` para Enhanced.
- No hay proido puro de rutas públic (todos los `Path.Combine` internos).
- Tests fakes `IClientInstallLocator` para no tocar el OS en las pruebas.

## Testing Decisions

- Tests sobre el seam (`FakeClientInstallLocator`, configurable de installments).
- Tests unitarios alrededor del `GetExecutablePath` y `IsInstalled` del `ClientInstallLocator` (usando `Path.Combine` más stubs locales). Es hecho PLAZA plano con el modelo de tests libre de filesystem.
- Suite completamente verde al final.

## Out of Scope

- Splash / reboot loop, cortesía de job, estado "installed vs ready/not-authed".
- Detección de carpeta personalizada o portable installs (`GetFolderPath` no la detecta; cubrimo default).