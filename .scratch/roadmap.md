# Roadmap

Status: active

Current release line: **v1.1** (phases 9-12 below). v1.0 = phases 1-8, all done.

Plan for the remaining phases of FiveMServerLauncher, in build order. Each phase follows the
repo workflow: `to-spec` -> `to-tickets` -> `implement` (TDD) -> `code-review` -> commit.
Phases listed as **Independent** have no ordering constraint and can be picked up at any time.

The roadmap is the plan of record; update the `Status:`/notes here as phases move.

## 1. Direct-open client (from the dropdown)

**Status:** ✅ done (commits `9a6d040` + `8cfa35c`, manual E2E verified).

**Spec:** `.scratch/direct-open/spec.md` (to write)

**Current state:** the OPEN dropdown is visual-only — `ClientOption_Click`
(`Views/MainView.xaml.cs`) swaps the button label and never launches anything. `MainViewModel`
has no open command; `GameLauncher.OpenClient(GameClient)` exists but is only reached for
`FiveMEnhanced` and never invoked by the UI. Legacy FiveM direct-open (no server address) has
no path at all.

**Goal:** pressing the dropdown client actually opens the selected client.

**Boundary (from DOC.md):** "Open FiveM / Open FiveM Enhanced" is a *secondary* action next to
"Enter server"; opening must work without requiring a server address. `FiveMEnhanced` only
*opens* (no `fivem://connect`, no `-b`/`-pure_`/`-cl2`).

**Unblocks:** phases 2, 3 and 4 hook into this flow.

## 2. Settings + auto-launch

**Status:** ✅ done (commits `e94acd2` + `121499c`, manual E2E verified).

**Spec:** `.scratch/settings-and-autolaunch/spec.md` (to write)

**Current state:** `LauncherSettings` (`PreferredClient`, `AutoLaunch`) and its storage
(`ISettingsStorage` -> `FileSettingsStorage` -> `ConfigurationRepository`) exist but nothing
reads them; there is no settings UI and no auto-launch behavior.

**Goal:** a small settings surface (preferred client + auto-launch) and the "open automatically
next time" behavior — remember the last used server address and auto-connect to it on a
subsequent launch when `AutoLaunch` is set.

**Boundary (from DOC.md):** `LauncherSettings` holds **global launcher preferences only** — never
per-server fields, never `Platform`, never `ServerPort`. Phase 1's dropdown default reads
`PreferredClient`.

## 3. Dev Mode

**Status:** ✅ done (commit `7b03a0a`, manual E2E verified).

**Spec:** `.scratch/dev-mode/spec.md` (to write)

**Current state:** `FiveMLaunchOptions` already models `GameBuild`, `PureMode`, `SecondClient` and
serializes `-b<build> -pure_<nivel> [-cl2]` via `ToCommandLineArgs()`, but nothing exposes them.

**Goal:** a separate dev-mode experience (DOC.md #Dev Mode) to open FiveM directly with a manual
Game Build, manual Pure Mode and/or a second client (`-cl2`).

**Boundary (from DOC.md):** these overrides apply **only when opening FiveM directly** — never
forced onto a server connection (server-published requirements always win). Needs a
`GameLauncher.OpenAsync` path that serializes a `FiveMLaunchOptions` to the Legacy CLI.

## 4. RedM direct-open

**Status:** ✅ done (commits `62e5403`, `bd65c82` + view fixes `81e8854`/`32799b4`;
manual E2E verified: dropdown, redm://connect, dev client toggle, CitizenFX.ini priming).

**Spec:** `.scratch/redm-open/spec.md` (to write)

**Current state:** `GameClient.RedM` exists and `CfxVars` maps CFX `gamename rdr3` to it, so
connect-by-address already works. There is no RedM option in the OPEN dropdown and no direct-open
path; `ClientInstallLocator` does not probe a RedM install.

**Goal:** RedM as a first-class open option, reusing the phase 1 flow; verify the RedM install
location/executable before assuming (DOC.md: "investigate before assuming").

## 5. UI improvement (with a design skill)

**Status:** ✅ done (phase close: code-review Standards + Spec, suite 313 green, visual
confirmation; commit pending `code-review` closure). Spec: `.scratch/ui-improvement/spec.md`
status `done`, issues 01–05 all resolved.

**Spec:** `.scratch/ui-improvement/spec.md`

**Goal:** polish the main UI against DOC.md (#UX, #Initial UI design) once the core flows are
wired, so the work lands on working behavior.

**Step 0 — design skill:** ✅ `.agents/skills/ui-design/SKILL.md` created (repo-local skill,
picked up automatically via `skills.paths` in `opencode.json`; no `skills-lock.json` entry — only
pinned external skills live there). The skill encodes:
- DOC.md UX philosophy: don't bother if everything is ready; show an explanatory progress screen
  only when something requires intervention or waiting.
- **Dark theme is the identity and stays** (user correction): greys `#101010`/`#181818`/`#202020`,
  amber accent `#F5A623`, deep borders `#080808` — not the light palette originally listed below.
- WPF/XAML conventions: clean design, white space, large buttons, restrained animations, styles
  in resources, bindings over code-behind, business logic never in XAML.

**Phase outcome:** layout clipping fixes, settings/dev mutual exclusivity, thin dark scrollbar,
saved-server rows (name + selected-only edit pencil, flush right, accent hover), add/edit overlay
dialog (address-keyed `Update` vs remove+add, in-dialog `DialogError`), multi-size `.ico`
(`Assets/launcher.ico`, `ApplicationIcon` + window `Icon`).

## Independent (out-of-band)

### Auto-update

**Spec:** `.scratch/auto-update/spec.md` (to write)

**Current state:** nothing implemented. DOC.md (#Updates) sketches a decoupled
`IUpdateService` (`CheckForUpdates` / `DownloadUpdate` / `InstallUpdate` / `RestartLauncher`)
and the self-replacement-while-running problem is documented but unsolved.

**Boundary:** launcher update is independent of FiveM update, server update and game update.
Never mix these concepts.

### Richer server-info UI

**Status:** ✅ done (tickets `.scratch/server-info-ui/` 01–05, suite 357 green, manual E2E
verified by the user).

**Spec:** `.scratch/server-info-ui/spec.md` (status `done`)

**Phase outcome:** `SavedServer.CfxId` enrichment key (captured at save, background for
IP:port/domain); `ServerEnrichmentService` (per-minute streamRedir presence + `gamename` → game
tag, icon version cache via `/single/` + `/icon/`); `MainViewModel` refresh loop + background
capture; saved rows render `[icon] name [gameTag] [status]` with per-game colors, `players/max`
online, `OFFLINE`/`UNRESOLVED` otherwise.

**Boundary (from DOC.md):** this "may evolve to show" server banner, online status, players,
news, Discord, website, CFX info — additive UI only, **without modifying the business
architecture**.

**Possible follow-ups (unticketed):** banner, news, Discord/website, CFX info for enriched rows.

## 6. CFX service status (FiveM / FiveM Enhanced / RedM)

**Status:** ✅ done (tickets `.scratch/cfx-status/` 01–05 resolved, suite 387 green, code
review clean, manual E2E verified).

**Spec:** `.scratch/cfx-status/spec.md` (status `done`)

**Current state:** the left panel hardcodes `FIVEM / CFX  ONLINE`, which is always ON and
carries no real information.

**Goal:** replace it with live three-row status (FiveM, FiveM Enhanced, RedM — dot + English
label) from the official statuspage (`summary.json`); FiveM/RedM use their dedicated components,
FiveM Enhanced uses the page-level indicator (no dedicated component exists). Refresh reuses the
existing per-minute loop + startup; outage keeps last known / `UNKNOWN`, never crashes.

## 7. Final project review + final release build

**Status:** ✅ done (tickets `.scratch/final-review/` 01–03 resolved; suite 422 green;
code review clean; final Release build 0 errors; one conventional commit).

**Spec:** `.scratch/final-review/spec.md`

**What:** project-wide audit (`find-untested-sources`) closed the genuine direct-test gaps:
`CfxStatusItem` plus the four legacy data-only converters (`GameClientToBrushConverter`,
`InverseBoolToVisibilityConverter`, `SelectionEqualityToVisibilityConverter`,
`BytesToImageSourceConverter`) gained direct tests; `CfxStatusServiceTests` got the missing
`Assert.NotNull` guards so the suite no longer carries CS8602/CS8604/CS8625 warnings. Real OS
seams (`ProcessStarter`, `DnsResolver`, `UriSchemeRegistration`, `UriShellStarter`,
`App.xaml.cs`, XAML code-behind) and internal infrastructure (`CfxVars`, `RelayCommand`,
`AsyncRelayCommand`, `DevLaunchParams`) stay untested-by-design (faked via seams, no
`InternalsVisibleTo`). Final release build: `dotnet build FiveMServerLauncher.slnx -c Release`
(0 errors; artifacts under `src/FiveMServerLauncher/bin/Release/net10.0-windows/`).

**Goal:** pin the last untested public logic types, review the closing diff, and produce the
final Release build.

## 8. Product name "CFX Launcher" + portable single-file build

**Status:** ✅ done pending commit (tickets `.scratch/product-name-and-portable/` 01–04 resolved;
suite 439 green; code review clean on both axes after the findings were fixed; single-file
publish verified; final manual smoke run left to the user — the machine-wide .NET update dialog
interrupts agent-driven launches)

**Spec:** `.scratch/product-name-and-portable/spec.md` (status `done`)

**What:** rename visible presentation to **CFX Launcher** (window title, title bar, footer, and
executable `CFXLauncher.exe` — cosmetic only, project/namespaces stay `FiveMServerLauncher`)
with a **by CodeRanger** credit; make the launcher portable: settings (`launcher-settings.json`)
and saved servers (`saved-servers.json`) stored **beside the exe** via a `PortableDataDirectory`
seam (+ one-time `%localappdata%` migration), and a self-contained **win-x64 single-file**
publish (`PublishSingleFile`, no trimming) as the documented release artifact.

**Goal:** shipping a single portable `CFXLauncher.exe` that carries its own data alongside it.

---

# v1.1

Four phases, in build order. Decisions were grilled and settled (see specs); the domain fix
rules are driven by a read-only spike against the real streamRedir snapshot (33,900 servers)
— key facts inline in the phase 9 spec.

## 9. Improvements & fixes (panel layout, dialog titles, port-less addresses, save-refresh)

**Status:** ✅ done (tickets `.scratch/v1-1-polish-and-fixes/issues/` 01–08 resolved; suite 474
green; code review clean on both axes after fixing matching order + ambiguity fall-through;
commit `66cb03a`). Visual verification of the compacted layout pending user eyeball.

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md`

**What:** (a) taller saved-servers list by compacting the connect-address area (smaller text,
removing wasted space above the ENTER SERVER button); (b) small field titles (Name / Address)
in the add/edit server dialog; (c) domain fix — accept port-less addresses (bare domain / bare
IP) and match them against the streamRedir catalog with data-driven rules (hostname
string-match first, DNS+default-port second, unvalidated fallback with default port 30120);
(d) saving a server in the dialog triggers an immediate data refresh instead of waiting for
the per-minute cycle.

## 10. Refresh button on the saved-servers list

**Status:** spec ready (`Status: ready-for-agent`).

**Spec:** `.scratch/saved-servers-refresh-button/spec.md`

**What:** a refresh button at the top of the saved-servers panel forces a fresh update of the
listed servers' data (bypassing the TTL cache), guarded by a shared cooldown so the expensive
streamRedir download can't be spammed. Depends on the forced-refresh seam from phase 9.

## 11. Saved-servers search bar

**Status:** spec ready (`Status: ready-for-agent`).

**Spec:** `.scratch/saved-servers-search/spec.md`

**What:** a search box above the saved-servers list that filters rows live as you type,
matching name and address (case-insensitive substring).

## 12. Server browser view

**Status:** spec ready (`Status: ready-for-agent`).

**Spec:** `.scratch/server-browser-view/spec.md`

**What:** a full content-area view swap (not a new window) listing all public CFX servers —
icon, name, game, players/max — with filters (game, hide full, hide empty) and name search;
per-server actions Connect (normal connect flow by cfx id, back to main view) and Save (adds
to saved servers, prefilling the dialog). Data loads TTL-respecting on open, plus a manual
refresh sharing the phase 9/10 cooldown. Explicitly out of scope: column sorting, favorites,
join history, detail pane.

**Depends on:** phases 9 and 10 (address matching + forced-refresh cooldown are shared).