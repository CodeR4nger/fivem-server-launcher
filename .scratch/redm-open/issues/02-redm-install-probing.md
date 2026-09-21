# 02: RedM install probing in the locator

**What to build:** `ClientInstallLocator` resolves a RedM install: registry
`HKCU\Software\CitizenFX\RedM` → `Last Run Location` first, default path
`%localappdata%\RedM\RedM.app\RedM.exe` fallback. Tests through the seam cover registry-first,
fallback, and not-installed.

**Blocked by:** none.

**Status:** resolved

- [ ] Registry value wins when present; default path used otherwise.
- [ ] Not installed → null path.
- [ ] Legacy/Enhanced probing unchanged.
