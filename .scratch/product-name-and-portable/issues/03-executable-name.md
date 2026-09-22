# 03: Executable named CFXLauncher.exe

**What to build:** the produced executable is `CFXLauncher.exe` (taskbar, on-disk, process
name) while the project, assembly, root namespace and every C# identifier keep their
`FiveMServerLauncher` identity — a cosmetic rename via MSBuild output naming only.

**Blocked by:** None (can start immediately).

**Status:** resolved (suite 437 green; exe is `CFXLauncher.exe`, namespaces untouched)

- [x] MSBuild output naming produces `CFXLauncher.exe` — via `<AssemblyName>CFXLauncher</AssemblyName>`
      + `<RootNamespace>FiveMServerLauncher</RootNamespace>` (namespaces preserved).
      Note: the spec-preferred `TargetName` route triggers a known .NET 10 WPF markup-compilation
      bug (MC3050: temp-project assembly name can't resolve `{x:Type}`); `AssemblyName` is the
      reliable alternative and keeps every C# identifier unchanged.
- [x] `dotnet build FiveMServerLauncher.slnx` emits `CFXLauncher.exe`; test project builds and
      the full suite stays green.
- [x] Debug output confirms `CFXLauncher.exe` (taskbar/process will show `CFXLauncher`).