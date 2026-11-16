# CFX Launcher

A fast, portable Windows launcher for FiveM servers. It handles all the boilerplate of getting
into a server: it reads what the server requires, prepares your client (game build, pure mode,
pool sizes, Steam/Discord readiness), and starts the right game — without needing admin rights
or a machine-wide install.

Built with C#/.NET 10 + WPF (MVVM), developed strictly with TDD.

## Features

- **Connect in one click** — supports every address form: CFX ID (`8e8xxv`), CFX URL
  (`cfx.re/join/<id>`), `IP:port` and `domain:port`.
- **Auto-configures the client** — applies the server's required **game build**
  (`-b<build>` / `CitizenFX.ini` priming) and **pure mode** (`-pure_<level>`), plus
  `PoolSizesIncrease` when the server publishes pool sizes.
- **Auto-detects the games** — detects installed FiveM (Legacy), FiveM Enhanced and RedM and
  launches the right one; a Connect to an Enhanced-only server opens the Enhanced client
  directly.
- **Prepares required apps** — if a server requires **Steam** or **Discord**, the launcher
  starts them and waits until they are *actually ready* (Steam fully logged in, not just
  running) before connecting.
- **Servers page** — save servers and see them with live status (players online, Offline,
  Not listed), per-game tag and server icon; filter by All / Online / Offline, search, and see
  the selected server's game build, pure mode and Steam/Discord requirements before you
  connect. Edit, shortcut, copy, reorder and delete from its ⋮ menu.
- **Browse page** — the full CFX server list with game, full/empty and name filters, sorting,
  and one-click Connect or Save.
- **CFX network status** — live FiveM / RedM / Enhanced status from the CFX status page right
  in the launcher.
- **Dev mode** (a Settings tab) — open FiveM or RedM directly with a specific game build / pure
  mode, and launch a second client.
- **Auto-launch** — optionally remembers your last server and connects automatically at startup.
- **Web links** — turn on the web links switch under Settings and links such as
  `cfxlauncher://connect/cfx.re/join/<id>` (any address form after `connect/`) open the server
  in the launcher, through the same validated connect as the Connect button — the
  running launcher picks it up if it is already open. The registration is per user
  (`HKCU\Software\Classes\cfxlauncher`, no admin), is re-pointed at startup if you moved the
  exe, and turning it off removes it. With several copies of the exe, the last one started with the
  option on owns the links.
- **Tray icon** — the launcher icon stays in the notification area while the app runs. Its
  right-click menu lists your saved servers (first 15, in your order; "All servers..." opens
  the window) for a one-click connect, plus Show and Exit; a left click shows the window. Turn on
  minimize to tray under Settings and minimizing hides the window from the
  taskbar until you bring it back from the icon, a shortcut, a web link or a second launch.
  Closing the window always exits.
- **Resizable, animated, keyboard-friendly UI** — a resizable window (minimum 1000×640) that
  remembers its size and position, three pages in a bottom bar, an update pill in the title
  bar, and short animations you can switch to Minimal under Settings.
- **Portable** — a single self-contained executable (~64 MB); settings are stored next to the exe.

## Usage

1. Download the published `CFXLauncher.exe` (a single file — no DLLs; the native WPF libraries
   are baked in and self-extract to a temp folder at startup).
2. Double-click it. No installation, no admin rights.
3. Pick a saved server (or paste an address) on the **Servers** page and press **Connect**, find
   one on the **Browse** page, or open a client directly with the **Open client** button in the
   bottom bar.

## Requirements

- Windows 10 / 11.
- One of the supported clients installed: FiveM (Legacy), FiveM Enhanced, or RedM — they are
  **auto-detected** from their standard install locations.
- No .NET runtime needed: the published exe is self-contained.

## Build & test

Requires the **.NET 10 SDK**.

```sh
dotnet restore src/FiveMServerLauncher/FiveMServerLauncher.csproj -r win-x64
dotnet test
dotnet build FiveMServerLauncher.slnx
```

## Publishing a portable single-file exe

Build first (note `SelfContained=true` is mandatory at *build* time — since .NET 8,
`-r win-x64` alone does not imply self-contained), then publish with `--no-build`:

```sh
dotnet build src/FiveMServerLauncher/FiveMServerLauncher.csproj -c Release -r win-x64 -p:SelfContained=true
dotnet publish src/FiveMServerLauncher/FiveMServerLauncher.csproj -c Release -r win-x64 --self-contained --no-build -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o dist
```

The result is a single `CFXLauncher.exe` (~137 MB, no DLLs to keep together). Optionally add
`-p:EnableCompressionInSingleFile=true` to shrink it to ~62 MB. See `AGENTS.md` for the rationale
behind this exact command sequence.

> **Note on antivirus**: the exe is **code-signed-blank** (no signing certificate, as is normal
> for a hobby project). Windows SmartScreen and some AV heuristics may show a warning. The code
> is open and builds reproducibly from this repo. If you distribute it, shipping the SHA-256 of
> the build lets recipients verify the file is the authentic build.

## Project structure

- `Core/` — shared enums and app info.
- `Configuration/` — global launcher settings (`launcher-settings.json`).
- `Domain/` — pure domain logic (server addresses, saved servers, server profiles, launch options).
- `Service/` — CFX API clients (server info, streamRedir catalog, status page), install detection.
- `Launch/` — game launching, external app readiness (Steam/Discord).
- `ViewModels/` — MVVM layer.
- `Views/` — WPF UI.
- `tests/` — xUnit suite (no mocking library; fakes and HTTP handlers).

Design decisions and UX philosophy live in `DOC.md`.

## Third-party font

The UI font is [Inter](https://github.com/rsms/inter) 4.1 by The Inter Project Authors, bundled
in `src/FiveMServerLauncher/Assets/Fonts/` (static weights Regular, Medium, SemiBold, Bold and
ExtraBold). It is licensed under the SIL Open Font License 1.1; the license text ships beside
the font files as `OFL.txt`.