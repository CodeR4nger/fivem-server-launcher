I am developing FiveMServerLauncher, a Windows launcher for FiveM written in C#/.NET 10 with WPF, using decoupled architecture and TDD with xUnit.

I want you to continue development from the current project state, respecting design decisions already made. I do not want you to rebuild or change established decisions unless there is a concrete technical reason and we discuss it first.

# Project goal
The goal is to create a simple, fast launcher for Windows that allows:
- Entering a FiveM server directly.
- Opening FiveM or FiveM Enhanced manually.
- Selecting the preferred client.
- Automatically preparing server-specific requirements.
- Detecting and launching external applications when needed.
- Getting server information from CFX.
- Applying the execution options the server requires.
- Having a dev mode with additional options.
- Auto-update itself in the future.
- Keeping logic completely separated from the UI.
- Requiring no administrator privileges.
- UX philosophy: if everything is ready, don't bother the user. If something requires intervention or waiting, show an explanatory loading/progress screen.

# Current architecture
The project is organised around independent responsibilities.

Configuration is already separated into:
```
Configuration
├── LauncherSettings
├── ConfigurationRepository
├── ISettingsStorage
├── InMemorySettingsStorage
└── FileSettingsStorage
```
``InMemorySettingsStorage`` lives in the tests project to avoid depending on the filesystem during tests.

``FileSettingsStorage`` is the real implementation and persists via JSON.

Persistence uses ``System.Text.Json`` and ``JsonStringEnumConverter``.

### Intentional seams (ConfigurationRepository and ServerRequirementsResolver)

Although ``ConfigurationRepository`` and ``ServerRequirementsResolver`` mostly delegate, they are kept deliberately as seams. Their boundary:

- ``ConfigurationRepository`` is the non-null API of the Configuration module: ``Load()`` always returns a ``LauncherSettings`` (applies ``?? new LauncherSettings()``) and validates input in ``Save``. It isolates the UI from storage nil-ness and is substitutable in tests with ``InMemorySettingsStorage``.
- ``ServerRequirementsResolver`` translates ``CfxServerInfo`` (Service, CFX shape) into ``ServerRequirements`` (Domain). Keeps Domain free of CFX conventions (``sv_enforceGameBuild``, ``sv_pureLevel``, ``requestSteamTicket``) and is directly testable without HTTP.

Inlining them would couple Domain or UI to CFX/storage details. Decision recorded in `docs/adr/0001-intentional-middle-men-seams.md`.

The test suite uses xUnit and is currently fully green.

The last known state was:
```shell
Test summary: total: 15; errors: 0; correct: 15; skipped: 0; duration: 1.2 s
Compilation succeeded in 3.4s
```
So don't break existing tests and continue applying TDD.

## Current LauncherSettings
After reviewing the initial design, LauncherSettings was simplified.
Today it must represent only global launcher preferences:
```
public class LauncherSettings
{
    public GameClient PreferredClient { get; set; } = GameClient.FiveM;
    public bool AutoLaunch { get; set; } = false;
}
```
Current enums:
```
public enum GameClient
{
    FiveM,
    FiveMEnhanced
}
```
The reason to drop Platform is FiveM/Rockstar manage the game platform and we don't want the launcher to depend on Steam/Epic to authenticate or start FiveM.

The reason for dropping ServerPort: the port belongs to the server/profile, not global config.

### Rockstar / Steam / Epic / Discord
An early design decision:

**RSC never managed by the launcher.**

FiveM needs Rockstar Social Club for sign-in and FiveM handles that itself.

Steam and Discord are different:
- Neither is required to start FiveM.
- Some servers run custom scripts requiring Steam or Discord.
- If those aren't ready, the user can get in FiveM and be rejected later.
- We want to detect/prepare those requirements before launching/connecting to FiveM.
So Steam and Discord are per-server ``ServerProfile`` requirements.

Nor do we assume that a process existing means the service is fully ready. The readiness model has two levels: `running` (coarse "process exists", used to decide whether to start the app) and `ready` (the gate the connect flow waits on). Steam `ready` is a **composite probe** — the `steam` process, the `steamwebhelper` UI-host process, and a logged-in session (`ActiveUser != 0` in the Steam `ActiveProcess` registry key) — so Steam is only "ready" once it has booted, restored its session, and signed the user in; the login screen keeps the helper running but no user, so it stays not-ready. Discord's `ready` is its process running.

## ServerProfile
We want to completely separate the launcher's global config from per-server config.

The current concept is:
```
ServerProfile
├── Name
├── Address
├── Requirements
└── FiveMLaunchOptions
```
### Name
Friendly name of the server.

### Address
Must be flexible.

We want to be able to work with:
```
CFX ID
CFX URL
IP:port
domain:port
```
Conceptual examples:
```
8e8xxv
cfx.re/join/xxxxx
149.56.120.52:30320
play.example.com:30120
```
We don't want to couple the launcher to a single format.

Where possible, a CFX address must resolve via the CFX API to get the server back.

# CFX API
We investigated the CFX API.

Relevant endpoints:
```
https://frontend.cfx-services.net/api/servers
https://frontend.cfx-services.net/api/servers/single/
https://frontend.cfx-services.net/api/servers/streamRedir/
https://frontend.cfx-services.net/api/servers/icon/
https://gss.cfx-services.net/v1/public/featured-servers
```
Public status endpoints:
```
https://citizenfx.statuspage.io/api/v2/status.json
https://citizenfx.statuspage.io/api/v2/components.json
https://citizenfx.statuspage.io/api/v2/incidents/unresolved.json
```
A ``/single/{CFX_ID}`` can return:
```
EndPoint
Data {
    hostname
    clients
    sv_maxclients
    server
    vars
    resources
    requestSteamTicket
    connectEndPoints
    ...}
```
Inside ``vars`` can appear very important info:
```
sv_defaultGameBuild
sv_enforceGameBuild
sv_pureLevel
sv_poolSizesIncrease
sv_enforceSteamAuth
sv_enhancedHostSupport
requestSteamTicket
```
An example, a real server we looked at returns:
```
sv_defaultGameBuild = 3258
sv_enforceGameBuild = 3258
sv_pureLevel = 1
```
and:
```
sv_poolSizesIncrease = "{...}"
```

We don't want to duplicate in ServerProfile info the server already publishes.

# FiveMLaunchOptions
``FiveMLaunchOptions`` must represent the options needed to prepare/launch FiveM.

## Verified investigation (sources: official docs and citizenfx/fivem source code)

Command line arguments documented officially (FiveM Shortcut):
```
-b<build>        → launch directly in a game build (ex: -b1604)
-pure_<level>    → launch directly in a pure mode 0-2 (ex: -pure_1)
-cl2             → second instance
```

Direct connection:
```
fivem://connect/<server>
fivem://connect/<server>?<params>
```

### How FiveM reads them (source code)
- ``PureModeState.h``: `GetPureLevel()` parses command line (`pure_`) via `CommandLineToArgvW` against `CfxState::initCommandLine`. **Pure mode is never persisted to CitizenFX.ini.**
- ``CrossBuildSwitch.cpp``: on connection to a server, the client orchestrates the switch itself: `RestartGameToOtherBuild(build, pureLevel, poolSizesIncreaseSetting, defaultBuild)` — handles the cross-build state.

### The cross-build problem
The connection-time switch flow is: the client is already open (build X) → prompts to join a server with different build → confirm dialog → **`RestartGameToOtherBuild` closes and re-opens the game**, making the required build.
This is a long, awkward cycle. The official `-b`/`-pure_` doc hedges: launch FiveM **directly** to the build/pure requested to **avoid the transition**.

### Role of ServerRequirements at launch
FiveM applies server requirements itself at connect, so the launcher's job isn't to to impose it, it's to **pre-load** it: launching with `-b<serverBuild> -pure_<level>` gets the client in the right state with no restart on connect.

Server-provided requirements win over manual config (including Dev Mode) when connected to a server, with one carve-out: a saved server's manual `Requires Steam` flag is *additive* — the player checked it, so the launcher treats Steam as required even when the server publishes `sv_enforceSteamAuth = false` (FiveM publishes the convar default `false`, which would otherwise silently neutralize a manual preference).

# CitizenFX.ini
We also investigated how FiveM persists part of this configuration.

The file lives next to the FiveM executable (usually ``%localappdata%\FiveM\FiveM.app\CitizenFX.ini``).

## Verified state
- The **only officially documented key** under ``[Game]`` besides ``IVPath`` is ``SavedBuildNumber=<build>``: launches FiveM directly in that build (same effect as ``-b`` but persistent).
- Example from a real ini:
```ini
[Game]
IVPath=D:\SteamLibrary\steamapps\common\Grand Theft Auto V
SavedBuildNumber=3258
PoolSizesIncrease={"AnimStore":20480,...}
ReplaceExecutable=0
UpdateChannel=beta
DefaultBuild=3258
```
- ``PoolSizesIncrease``, ``DefaultBuild``, ``ReplaceExecutable`` **exist de facto but are not a documented public API**. Pure mode is **not** persisted here (only read from the command line).

## Design consequence
- The primary and consistent way to pre-load build/pure is **command-line arguments** (the ``fivem://connect`` also accepts params).
- ``CitizenFX.ini`` is normally out of scope: we don't touch the user's ini except for one pre-connect case (see #Pool Sizes below) — priming ``[Game] DefaultBuild``/``PoolSizesIncrease`` into the Legacy FiveM client when connecting to a CFX-validated server that publishes those facts. The client manages pool sizes itself via `IncreasePoolSize` / `PoolSizeManager` on connect; priming only skips that restart-on-connect dance.


# Dev Mode
We want a Dev Mode separate from the normal experience.

It must allow options that shouldn't belong in the user's normal configuration:

- Second FiveM client (-cl2)
- Configure Game Build manually
- Configure Pure Mode manually

Important clarification:
The ``Game Build`` and ``Pure Mode`` configurable in Dev Mode are for opening FiveM directly, not for forcing those values upon connection to a server.

When connecting to a server, the server's requirements/settings prevail.

# Pool Sizes
We've confirmed some servers publish:

sv_poolSizesIncrease

with a pool JSON, e.g.:
```
{
  "AnimStore": 20480,
  "AttachmentExtension": 430,
  "CMoveObject": 600,
  "CWeaponComponentInfo": 2048,
  "EntityDescPool": 20480
}
```
We don't want to treat this JSON as arbitrary launcher configuration.

## Verified state
- FiveM manages pool sizes **internally at connect**: the client receives the request and applies it via ``PoolSizeManager`` / ``IncreasePoolSize``, without launcher or ini intervention (see ``CrossBuildSwitch.cpp`` and ``PoolSizesState.h`` in citizenfx/fivem).
- Pool limits are served from ``content.cfx.re``.

## Design consequence
Pool sizes **stay out of FiveMLaunchOptions**: the ``sv_poolSizesIncrease`` JSON is not a launch argument. Pre-connect, the launcher primes it verbatim into the Legacy FiveM ``[Game] PoolSizesIncrease`` (best-effort, only when the current value differs) so the client boots with the right pool sizes instead of restarting on connect; when the server runs default pool sizes the key is reset to an empty value, clearing stale increases. We keep the JSON in domain only to interpret server facts, never as launcher configuration.

# FiveM / Enhanced client
The launcher supports:
- FiveM
- FiveM Enhanced

Global preference:
- PreferredClient

Main UI will have:
- Enter a server as primary action.
- A secondary button with dropdown for:
- Open FiveM
- Open FiveM Enhanced
Conceptually:
```
┌───────────────────────────────┐
│          ENTER SERVER         │
└───────────────────────────────┘

┌───────────────────────────────┐
│         OPEN FIVEM       ▼    │
└───────────────────────────────┘
```
The dropdown lets you select the client. It lists only the clients installed on the machine (checked at startup), and pressing the open button launches the selected client directly. Connecting to a FiveM Enhanced server also opens the Enhanced client directly (no URI protocol on Enhanced).

## FiveM Enhanced: verified state (investigated before assuming)
- **It's a separate client and launcher**: downloaded separately (fivem.net), installed in its own folder and does **not** document `fivem://connect`, `-b`, `-pure_` or `-cl2`.
- **`-cl2` doesn't exist in Enhanced** (official doc "Running two FiveM clients"): a second instance needs server side `sv_devMode true` and then "Launch Additional Client" from devtools (F8). No CLI argument.
- **`+set moo 31337` was removed**: Enhanced devmode is server side (`sv_devMode true`), not command line.
- **Pure mode always on** in Enhanced ("can no longer be turned off"); no `-pure_X`.
- **Supports only the latest gamebuild** (Kortz Center Heist); no builds to pin with `-b`.
- **The connection flow was redesigned** (Development Update #2): no longer starts the game in background and **the client restart is removed** on joining a server with different config. The Legacy cross-build restart problem doesn't exist on Enhanced.

## Design consequence
- Direct connection serialization (`fivem://connect/<addr>`) and `-b`/`-pure_`/`-cl2` args apply to **FiveM (Legacy) and RedM**. For **FiveM Enhanced**, the launcher can only **open the client**; connecting happens inside its own UI. A `FiveMLaunchOptions` with `GameClient = FiveMEnhanced` doesn't serialize a URI nor connection args.

# UX
The main philosophy is: don't bother if everything is ready.

If Steam and Discord are ready:
```
PLAY
 ↓
Steam ✓
Discord ✓
FiveM ✓
 ↓
Connect
```
without showing unnecessary windows.

If something needs intervention:
```
Preparing the game...

Steam
█████████████░░░░

Starting Steam...
```
Then:
```
Steam ✓
Discord ✓
FiveM ✓
Connecting...
```
We want a light splash screen because our current operations are fast, but in the future they could include:
```
launcher update,
CFX query,
detection,
FiveM preparation,
validations,
other operations.
```
Desired architecture:
Don't put logic directly in MainWindow.xaml.cs.

The flow belongs conceptually to:
```
UI
 ↓
Application / Manager
 ↓
Domain services
 ↓
Infrastructure
```
We want separate responsibilities:
```
GameLauncher
ProcessManager
StartupManager / Checker
PathResolver
Server service
CFX API service
Settings repository
Settings storage
Detection
Updates
```
The architecture must let us test logic without launching real processes or depending on the real filesystem when not needed.

# TDD
We're developing with TDD.

The working rule is:
```
RED
 ↓
Minimal implementation
 ↓
GREEN
 ↓
Refactor
```
Config/storage tests are currently green.

When you propose the next step:
- first explain briefly what behavior we're adding;
- tell me which test we should create;
- let it fail;
- implement the minimum;
- run dotnet test;
- fix until GREEN;
- then refactor if needed.
Don't advance multiple architecture layers at once.

We want to build incrementally.

# Project's actual design
We dropped the old design that treated Steam/Epic as global platforms.

The current design is:
```
LauncherSettings
 ↓
global preferences

ServerProfile
 ↓
server config/requirements

CFX API
 ↓
dynamic server info

FiveM preparation
 ↓
apply what the server needs

FiveM
 ↓
connection
```
The launcher doesn't assume the server needs Steam, Discord, a specific Game Build, Pure Mode or Pool Sizes.

It must determine that from the server info wherever possible.

# Initial UI design
The initial main screen must be minimalist:
```
┌─────────────────────────────────────────┐
│                                         │
│          FiveMServerLauncher             │
│                                         │
│                                         │
│       [ ENTER THE SERVER ]              │
│                                         │
│       [ OPEN FIVEM              ▼ ]     │
│                                         │
│       ☑ Open automatically               │
│          next time                       │
│                                         │
│                           ⚙ Settings      │
└─────────────────────────────────────────┘
```
**Colors:**
```
Background: #FFFFFF
Primary:    #000000
Accent:     #FF6A00
Text:       #111111
Secondary:  #777777
Error:      #D32F2F
Success:    #2E7D32
```
Design must be clean, with plenty of white space, large buttons and restrained animations.

It may evolve to show:
- server banner,
- online status,
- players,
- news,
- Discord,
- website,
- CFX info,
without modifying the business architecture.

# Updates
The launcher update system must be decoupled.

Conceptually:
```
IUpdateService
    CheckForUpdates()
    DownloadUpdate()
    InstallUpdate()
    RestartLauncher()
```
Launcher update is independent of:
```
FiveM update
Server update
Game update
```
Don't mix these concepts.

The updater will eventually need to update/replace the launcher without the main executable trying to replace itself while running.

# Constraints
- Windows.
- .NET 10.
- WPF.
- MVVM.
- No administrator privileges.
- No coupling to UI.
- Keep tests fast.
- Don't run real processes in unit tests.
- Don't add abstractions "just in case".
- Keep abstractions that bring testability or separate real responsibilities.
- Don't re-introduce Platform.
- ServerPort doesn't belong in LauncherSettings.
- RSC not managed by the launcher.
- Steam/Discord are per-server, not universal FiveM requirements.
- Don't conflate "process started" with "service authenticated/ready".
- Don't force server-type Game Build/Pure Mode when the user just opens FiveM in Dev Mode.
- Prefer CFX-provided info to duplicated manual config.
# Next objective
Given the current state, continue designing and implementing the ServerProfile system and its relation to CFX info, without jumping to UI.

The next phase focuses on domain and tests:
```
ServerProfile
    ↓
Requirements
    ↓
FiveMLaunchOptions
    ↓
CFX server information
```
Before implementing, analyze the models/abstractions really needed and which is over-engineering.

Keep the TDD focus and advance incrementally.

If a technical decision depends on current FiveM/CFX info, investigate before assuming.