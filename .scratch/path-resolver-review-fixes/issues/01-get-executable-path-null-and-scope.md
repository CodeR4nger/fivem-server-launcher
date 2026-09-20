# 01: GetExecutablePath null when not installed + remove RedM mapping

**What to build:** spec fix: real `ClientInstallLocator.GetExecutablePathAsync` must return null if not installed (it should not return the expected path if the file doesn't exist). Remove scope creep: `GameClient.RedM` mapping deleted (the spec doesn't ask for it). Tests against the real one — injectable `Func<string,bool>` ctor seam — with no real filesystem involvement.

**Blocked by:** None

**Status:** resolved

- [x] `ClientInstallLocator` with injectable `Func<string,bool>` ctor (default: `File.Exists`).
- [x] `GetExecutablePathAsync` returns null when not installed.
- [x] `IsInstalledAsync` uses `File.Exists` (not `Path.Exists`).
- [x] RedM mapping deleted (scope creep).
- [x] Suite green (117).

## Comments
