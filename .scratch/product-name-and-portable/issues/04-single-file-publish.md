# 04: Self-contained win-x64 single-file publish + doc

**What to build:** `dotnet publish` produces one portable, self-contained **win-x64** single
`.exe` (`CFXLauncher.exe`, framework included, no trimming — WPF + `System.Text.Json` need
reflection) as the repeatable release artifact. The command is documented in AGENTS.md, and a
manual end-to-end run of the published exe confirms it runs standalone and its settings/servers
files land beside it (portable data working end-to-end).

**Blocked by:** 02 (portable data directory), 03 (exe name).

**Status:** resolved (single-file publish + AGENTS.md + partial E2E; final manual smoke run pending on the user's machine — the machine-wide .NET update dialog interrupts agent runs)

- [x] Publish command in AGENTS.md:
      `dotnet publish src/FiveMServerLauncher/FiveMServerLauncher.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o dist`
      IMPORTANT: run a `dotnet build -c Release -r win-x64 -p:SelfContained=true` FIRST, then
      publish with `--no-build` (a publish that recompiles WPF markup in the `_wpftmp` temp
      project cannot resolve the Grpc.Tools-generated `Master` types — CS0246). `-r win-x64`
      alone does NOT imply self-contained (.NET 8+ breaking change); a framework-dependent
      build reused by `--no-build` would publish an exe that REQUIRES the machine's .NET
      runtime (verified: 227 KB vs 140 MB, and the prompt-to-update bug the user hit).
      Single-file props live on the command line only (in the csproj they break Release builds
      the same way).
- [x] A publish run produces a single `CFXLauncher.exe` (~140 MB, self-contained, one file; no
      sibling native DLLs or PDB once `IncludeNativeLibrariesForSelfExtract` is set).
- [x] Partial E2E: the published exe launched and stayed running (no startup crash), and the first
      (pre-refactor) build migrated the existing `%localappdata%\FiveMServerLauncher` files next to
      itself in `dist\` (portable wins, legacy kept as backup).
- [x] Code-review fixes applied: `LegacyDataMigration.Migrate` now takes the portable destination
      paths from `PortableDataDirectory` (single owner of the file names — no duplicated `KnownFileNames`)
      and swallows `IOException`/`UnauthorizedAccessException` per file (best-effort never crashes
      startup); injectable `Action<string,string>` copy seam for tests; AppInfoTests pins the exact
      `Title` string.