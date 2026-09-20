# 01: IClientLocator seam + real implementation

**What to build:** `Service/IClientInstallLocator` (seam) with `Task<bool> IsInstalled(GameClient)` and `Task<string?> GetExecutablePath(GameClient)`. Production implementation `Service/ClientInstallLocator` that resolves the real default paths:
- Legacy: `%localappdata%\FiveM\FiveM.app\FiveM.exe`
- Enhanced: `%localappdata%\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe`

No filesystem tests (seam covers everything).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Service/IClientInstallLocator` with two injectable methods.
- [x] Real `ClientInstallLocator` resolves `%localappdata%\FiveM` (Legacy) and `%localappdata%\FiveM for GTAV Enhanced` (Enhanced).
- [x] Tests via seams: 4 fake scenarios in `ClientInstallLocatorTests`.
- [x] Suite green.

## Comments
