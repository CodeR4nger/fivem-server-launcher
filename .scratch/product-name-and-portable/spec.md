# App product name "CFX Launcher" + portable single-file build

**Status:** done (tickets 01–04 resolved; suite 439 green; code review clean on both axes after
findings fixed; single-file publish verified; final manual smoke run left to the user)

## Problem Statement

The application presents itself as "FiveM Server Launcher", which undersells what it has
become — a launcher that opens FiveM, **FiveM Enhanced** and **RedM**, connects to CFX servers,
and shows live CFX status. The name should be a technical, representative product name, and
credit its author. Separately, the delivered artifact is a framework-dependent build folder with
a `.dll`, `.deps.json`, `.runtimeconfig.json` and protobuf runtime — the user wants a portable,
self-contained **single `.exe`** they can copy anywhere, with the launcher's own data
(settings + saved servers) stored **beside the executable** rather than in `%localappdata%`.

Decisions already locked with the user:
- Product name: **CFX Launcher**.
- Rename scope: **visible only** — window title + executable name; project, assembly,
  namespaces, solution and internal identifiers stay `FiveMServerLauncher`.
- Author credit: **by CodeRanger**, in the title bar and a footer.
- Portable meaning: **single self-contained `.exe` AND settings/servers stored beside it**.
- Settings/servers keep their current file names, only their location moves.
- Build target: **win-x64 only**.
- Seams: two new shallow seams, `AppInfo` and `PortableDataDirectory` (confirmed).

## Solution

1. **Rename (visible only).** The window `Title`, the custom title-bar text, and a new footer
   show **CFX Launcher** with the **by CodeRanger** credit. The produced executable is
   `CFXLauncher.exe`. Nothing internal is renamed: project name, assembly name, root namespace,
   solution file and every type stay `FiveMServerLauncher` — the exe name is controlled via
   MSBuild output naming only, so the C# project itself keeps its identity.
2. **Portable data.** The data directory currently hardcoded in `App.xaml.cs`
   (`%localappdata%\FiveMServerLauncher`) becomes **the directory that contains the running
   executable** (`launcher-settings.json` and `saved-servers.json` land next to the exe).
   Because the app runs as a single-file publish, `AppContext.BaseDirectory` is the temp
   extraction directory, **not** the exe's home — we resolve the true path from the running
   process. A small `PortableDataDirectory` seam owns that resolution so it can be tested
   without a real process.
3. **Single-file publish.** Add a self-contained win-x64 single-file publish configuration
   (no trimming — WPF + reflection), verified by publishing and running the built exe. The
   launcher already has no install step; this makes the single `.exe` fully portable.

## User Stories

1. As a user, I want the launcher window to say **CFX Launcher**, so the product name matches
   what it actually does (FiveM, FiveM Enhanced and RedM).
2. As a user, I want my author credit **by CodeRanger** visible in the window title area, so the
   launcher is clearly attributed.
3. As a user, I want a small **by CodeRanger** credit footer, so the authorship is reinforced
   without cluttering the main UI.
4. As a user, I want the executable file to be named **CFXLauncher.exe**, so it is identifiable
   on disk and on the taskbar.
5. As a user, I want the launcher to ship as a **single portable `.exe`** that runs standalone
   (framework included), so I can copy it to a USB stick or any folder and run it without
   installing .NET.
6. As a user, I want my settings and saved servers stored **next to the exe**, so the whole
   launcher — program and its data — travels together as one portable unit.
7. As a user, I want the existing saved servers and settings to keep working after the change,
   so nothing I saved under the old `%localappdata%` location is silently lost or broken.
8. As a developer, I want the exe/product rename to stay cosmetic, so the project, assembly,
   namespaces and all existing code keep their `FiveMServerLauncher` identity untouched.
9. As a developer, I want the "where does my data live" decision owned by a testable seam, so I
   can cover it without touching the real build output directory.
10. As a developer, I want the single-file publish command to be the documented, repeatable way
    to produce the deliverable, so the release artifact is reproducible.

## Implementation Decisions

### Product name source: `AppInfo`

- New `Core/AppInfo` static class: `ProductName` = "CFX Launcher", `Author` = "CodeRanger",
  and a convenience `Title` combining them for the window title bar and footer (single source
  of truth for user-facing strings).
- `MainWindow.xaml` binds `Title` and the title-bar `TextBlock` to `AppInfo` values (via
  `x:Static`), and a new footer `TextBlock` shows the credit line.
- The two hardcoded `FiveM Server Launcher` strings in `MainWindow.xaml` are the only user-
  visible occurrences changed; graticule: `CFX Launcher` / `by CodeRanger` appear nowhere in
  code except `AppInfo`.
- No UI code-behind added; the footer is static XAML bound to `AppInfo`.

### Portable data: `PortableDataDirectory`

- New `Configuration/PortableDataDirectory` seam. Constructor takes an injectable
  `Func<string?>` process-path provider; the default factory param uses
  `Environment.ProcessPath` (the real exe path even under single-file extraction). Tests inject
  a fake provider, so no real process is needed.
- Exposes the base data directory (the exe's folder, i.e.
  `Path.GetDirectoryName(provider())`) and composes the two file paths the composition root
  already passes: `saved-servers.json` and `launcher-settings.json` (names unchanged).
- `App.xaml.cs` composition root swaps its hardcoded `AppDataDirectory` for the seam's result.
- Edge: if the provider returns `null`/whitespace (no process), fail fast with a clear argument
  exception rather than falling back silently to a misleading location.

### Single-file publish

- Add publish properties (e.g. in the csproj or a requested publish profile):
  - `RuntimeIdentifier = win-x64`
  - `SelfContained = true`
  - `PublishSingleFile = true`
  - `PublishTrimmed = false` (explicitly off: WPF + `System.Text.Json` reflection need it)
  - `DebugType = none` and `DebugSymbols = false` for a clean deliverable
- Documented command (added to AGENTS.md Commands):
  `dotnet publish FiveMServerLauncher.slnx -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
- Manual close-out verification: run the produced `CFXLauncher.exe`, confirm settings/servers
  files appear **beside it**, and confirm the old `%localappdata%\FiveMServerLauncher` data is
  still loaded (see story 7).

### Migration of existing `%localappdata%` data (user story 7)

The current settings/servers live under `%localappdata%\FiveMServerLauncher`. Moving storage to
portable means new data lands beside the exe. To honor story 7 without complexity creep:

- On first load with a portable data dir that has **no** files but the old `%localappdata%`
  location **does** have them, copy the old files into the portable dir (settings
  `launcher-settings.json`, servers `saved-servers.json`), then read from there.
- This is a one-time, no-dialog migration owned by the composition root (best-effort copy; if
  the copy would overwrite a newer portable file, portable wins). The old location is left
  untouched after the copy (a backup), never deleted automatically.

The migration is thin glue in the composition root and is covered by a focused unit test for
the copy decision (old exists + new absent -> copied; new already present -> new wins). Details
of *which* storage class writes the final files are unchanged.

### Out of scope

- No rename of the project file, assembly, root namespace, solution, folders, or any C#
  identifier — strictly cosmetic output naming.
- No installer, no MSIX, no code signing, no icon redesign (existing `launcher.ico` stays).
- No trimming, no AOT, no ARM64/x86 publish targets.
- The `%localappdata%` files are copied, never deleted; leftover data is a backup, not a bug.

## Testing Decisions

- **A good test** asserts external behavior: the seam resolves the right directory/file paths
  from an injected path, and the migration copy decision chooses portable-over-legacy correctly.
  No real process, no real `%localappdata%`, no real build output is touched.
- **`AppInfo`** — trivial constants; a single test pins the exact `ProductName`/`Author`/
  `Title` strings so the visible name cannot drift (mirrors `InstalledClientOption` tests).
- **`PortableDataDirectory`** — table-driven: given a provider returning `C:\Apps\CFXLauncher.exe`,
  the data dir is `C:\Apps` and the two file paths are exactly `launcher-settings.json` and
  `saved-servers.json` under it; whitespace/null provider throws the fail-fast exception.
  Prior art: `ClientInstallLocator` (injectable `Func` seam) and `TempSettingsDirectory`/temp-path
  tests in `Configuration`.
- **Migration decision** — a small pure helper (e.g. `MigrateLegacyData(oldDir, newDir)`) tested
  with temp directories (mirroring `TempSettingsDirectory`): copies when new is absent, new wins
  when both exist, no-op when old absent.
- **UI strings** are not unit-tested via WPF; they are pinned through `AppInfo` and the XAML
  binds to it. The publish step is verified manually (run the published exe) — same
  close-out style as prior phases' "manual E2E verified".

## Further Notes

- `Environment.ProcessPath` is the correct real-path source for single-file .NET apps;
  `AppContext.BaseDirectory` under `PublishSingleFile` points at the temp extraction dir and
  must not be used for persistence (this is the trap this phase explicitly avoids).
- `PublishSingleFile` still extracts some framework-level bits to temp at startup internally;
  the user-visible data files land beside the exe via `PortableDataDirectory`. The exe is
  portable in the copy-it-anywhere sense; it is not a single native binary with zero runtime
  footprint.
- After this phase, the roadmap gains a "Product name + portable single-file build" entry and
  AGENTS.md Commands gains the publish command.