# 03: Resolve legacy install location from registry + portable layout

**What to build:** real `ClientInstallLocator` could not find a non-default install: priming skipped on a zip/custom layout. Fix:
- Legacy FiveM install dir: registry `HKCU\Software\CitizenFX\FiveM` -> `Last Run Location` first, `%localappdata%\FiveM\FiveM.app` fallback (`Func<string?>` injectable seam, default real registry).
- Installed probe accepts the launcher exe either inside the `.app` dir (installer layout) or as its direct sibling (portable/zip layout, e.g. `D:\My Games\FiveM\FiveM.exe`).
- Enhanced install dir corrected: `%localappdata%\FiveM for GTAV Enhanced\` (exe directly in folder, no `.app` subfolder).

**Blocked by:** None

**Status:** resolved

- [x] RED: injectable registry provider ctor; tests for registry dir (trailing backslash/space trimmed), portable sibling exe, Enhanced default dir; existing real-impl tests upgraded to hermetic `() => null` provider.
- [x] GREEN: `ClientInstallLocator` resolves registry-first, normalizes the dir, probes inside/sibling exe; Enhanced path without `.app`.
- [x] Verified on the real machine: registry `Last Run Location` = `D:\My Games\FiveM\FiveM.app\` -> ini present + sibling exe present.
- [x] Suite green (160 = previous 155 + 5 new).

## Comments
- Root cause of "CitizenFX.ini never modified": `CitizenFxPreparer` best-effort skips when `GetInstallDirectoryAsync` returns null; locator hardcoded `%localappdata%` and required `FiveM.exe` inside `.app`.