Status: ready-for-agent
Type: spec

# Direct-open client (OPEN dropdown actually opens the installed client)

## Problem Statement

The OPEN dropdown on the main view is decorative: picking "FiveM" or "FiveM Enhanced" only swaps
the button label (`ClientOption_Click` reads the `CommandParameter` and rewrites the button
`Content`); nothing is ever launched, and `MainViewModel` has no open action at all. Separately,
connecting to a `FiveMEnhanced` server returns `LaunchResult.OpenClient` but the launcher then
only shows "Opening FiveMEnhanced..." — the client is never actually opened, violating DOC.md's
verified consequence that "the launcher can only *open the client*" for Enhanced.

The building blocks already exist but are not wired: `ClientInstallLocator` resolves install dirs
and executable paths for Legacy FiveM (`%localappdata%\FiveM\FiveM.app\FiveM.exe`) and Enhanced
(`%localappdata%\FiveM for GTAV Enhanced\FiveM.exe`); `GameLauncher` orchestrates launches behind
`IGameProcessLauncher`; the only thing missing is a real "open this client executable" action and
a VM/XAML surface that exposes installed clients.

## Solution

An Application-layer open action, plus a data-bound dropdown that only offers **installed**
clients:

- `GameLauncher.OpenAsync(GameClient)` resolves the client executable via the install locator,
  delegates the real start to the process seam, and reports the outcome as a `LaunchResult`
  (`OpenClient` when launched, `NotInstalled` when no executable is found, `StartFailed` on a
  launch exception).
- The OPEN dropdown lists only the installed clients (checked against the install locator when the
  view loads). The open button shows the currently selected client and presses trigger the open
  action. If nothing is installed the open control reflects that (disabled, no fake options).
- Connecting to a `FiveMEnhanced` server now opens the client through the same primitive instead
  of returning an inert `OpenClient`.

## User Stories

1. As a player, I want to press the OPEN button and have the selected client actually launch, so
   that the primary action next to ENTER SERVER does real work.
2. As a player, I want the dropdown to offer only the clients that are installed on my machine, so
   that I never choose a client that cannot open.
3. As a player, I want the OPEN button label to follow my dropdown selection, so that I can see at
   a glance which client will open.
4. As a player, I want opening a client to be a distinct, non-blocking action independent of
   entering a server, so that I can boot FiveM without a server address.
5. As a player, when I connect to a FiveMEnhanced server, I want the Enhanced client to actually
   open (connection happens inside the client), so that the connect flow matches DOC.md's design
   consequence.
6. As a player, if a client that was installed is no longer found at open time, I want a clear
   English message instead of a silent no-op.
7. As a developer, I want all launch decisions (URI connect and executable open) to stay behind the
   single orchestrator `GameLauncher`, so the VM stays a thin caller.
8. As a developer, I want all real process starts to stay behind the existing process seams, so
   tests never touch real processes.
9. As a developer, I want the dropdown logic to be data-bound, so the label-swapping code-behind
   handler is removed and business state stays in the view model.
10. As a developer, I want the OPEN action guarded by the same busy state as connect, so repeated
    presses while working cannot double-launch.

## Implementation Decisions

- **Behavior seam (highest, existing):** `GameLauncher.OpenAsync(GameClient)`. `GameLauncher`
  already receives the two launch seams; it now also receives the install locator
  (`IClientInstallLocator`) so the open action can resolve the executable. The VM calls
  `OpenAsync`; it never resolves installs or starts processes itself.
- **Process seam (existing, one new method):** the game-process seam gains a start-by-executable
  method alongside the existing URI start, so both connect (URI) and open (path) keep exactly one
  game-process seam. The real implementation starts the executable through the existing process
  starter; like the URI path it returns once the process is handed off ("process started" is not
  "ready").
- **Result contract:** `LaunchResult` gains `NotInstalled(GameClient)`. `OpenClient(GameClient)`
  changes meaning from "the UI must open it later" to "client launched". Settled in one place: the
  VM's result-to-status mapping.
- **`GameLauncher.ConnectAsync`:** the no-URI branch (Enhanced, or an unaddressed client) now
  invokes the same open primitive instead of returning a deferred `OpenClient`, so Enhanced connects
  really open. Outcomes surface as `OpenClient`/`NotInstalled`/`StartFailed`.
- **View model:** gains `OpenClientCommand`, a selected-client property, and a dropdown data source
  built from the install locator listing the installed clients with their display names (FiveM /
  FiveM Enhanced). The selected client defaults to the first installed (Legacy first). The open
  command is enabled only while not busy and only when a client is selected. `NotInstalled` maps to
  an English status message; `OpenClient` keeps the existing "Opening {client}..." copy.
- **XAML:** the OPEN control becomes data-bound to the view model (items = installed clients,
  button content = selected client's display name, arrow toggles the list). The label-swapping
  code-behind handler is removed; any pure-visual toggle (show/hide the list) may stay in the view.
- **Defaulting note:** until the settings phase lands, the open default is first-installed /
  desktop default (FiveM); the settings phase will seed it from `LauncherSettings.PreferredClient`.
- **RedM:** out for now; the list is driven by the install locator, so a RedM option is a future
  candidate, not a special case.

## Testing Decisions

- Good test = external behavior through seams: what the VM shows and what the launcher does, never
  static calls or XAML internals.
- Prior art: `GameLauncherTests` with `FakeGameProcessLauncher` (records started URIs; extended to
  record executable starts), `FakeClientInstallLocator`, and `MainViewModelTests` through the
  `CreateViewModel` helper — naming `<Method>_Should<Expectation>`, Given/When/Then.
- Modules tested at their public seams: `GameLauncherTests` for `OpenAsync` outcomes and the
  Enhanced-connect open; `MainViewModelTests` for the dropdown source (installed only), selection,
  busy guard, status text, and open-command flow.
- Key cases: installed Legacy and Enhanced → seam started with the resolved executable path and
  `OpenClient`; missing executable → `NotInstalled` and seam not called; throwing start →
  `StartFailed`; Enhanced connect → seam started with the Enhanced executable path; VM lists only
  installed clients; open after settings default is out of scope but the command behaves for any
  selected client.

## Out of Scope

- Settings / `PreferredClient` seeding of the default selection (phase 2).
- RedM direct-open (phase 4) and any install-locator change for RedM.
- Installing or downloading missing clients; the "install" flow lives in FiveM.
- Dev Mode / `-cl2` / manual Game Build / Pure Mode overrides for direct open (phase 3).
- Auto-update and richer server-info UI.
- Any change to URI connect behavior, address classification, or CFX vars.

## Further Notes

- The dropdown defaulting is temporary until phase 2 wires `LauncherSettings.PreferredClient`;
  that phase must flip the initial selected client to the setting.
- DOC.md (#FiveM / Enhanced client) describes the OPEN action as secondary next to ENTER SERVER
  and the "open the client afterwards" responsibility for Enhanced — this phase makes the UI match
  that design consequence.
- Legacy direct-open launches the resolved `FiveM.exe` directly (not via `fivem://`, which needs a
  server address). Manual E2E must confirm the direct exe start boots Legacy FiveM cleanly; the
  launch-shell-context lesson (explorer routing for `fivem://` URIs) does not apply to a direct
  executable start, but verify on the real machine.