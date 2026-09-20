# 01: IClientLocator seam + real implementation

**What to build:** `Service/IClientInstallLocator` (seam) con `Task<bool> IsInstalled(GameClient)` y `Task<string?> GetExecutablePath(GameClient)`. Productas implementation `Service/ClientInstallLocator` que resuelve las rutas real defects por defecto:
- Legacy: `%localappdata%\FiveM\FiveM.app\FiveM.exe`
- Enhanced: `%localappdata%\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe`

Sin tests de filesystem (todoslos seam resuelto).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Service/IClientInstallLocator` con dos métodos inyectables.
- [x] `ClientInstallLocator` real resuelve `%localappdata%\FiveM` (Legacy) y `%localappdata%\FiveM for GTAV Enhanced` (Enhanced).
- [x] Tests via seams: 4 escenarios de fake en `ClientInstallLocatorTests`.
- [x] Suite verde.

## Comments