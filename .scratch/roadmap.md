# Roadmap

Status: active

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

**Spec:** `.scratch/server-info-ui/spec.md` (to write)

**Current state:** data sources exist (`CfxService`, `ServerCatalog`) but the UI shows no server
details.

**Boundary (from DOC.md):** this "may evolve to show" server banner, online status, players,
news, Discord, website, CFX info — additive UI only, **without modifying the business
architecture**.