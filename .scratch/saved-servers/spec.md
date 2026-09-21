Status: ready-for-agent
Type: spec

# Per-server Steam/Discord requirements via saved servers

## Problem Statement

Some FiveM servers run custom scripts that require the player to be running Steam or Discord before connecting; without them the player is rejected *after* loading into FiveM ("Unable to find SteamID...", "No Steam authentication ticket provided while this server enforces authentication with Steam", server-side Discord membership checks). The launcher can't assume a server needs Steam/Discord (`DOC.md`: "The launcher doesn't assume the server needs Steam, Discord..."), and today it has no way to know a server's requirements beyond game build/pure mode/`requestSteamTicket`.

Investigation (verified against FiveM source, `code/components/citizen-server-impl/src/SteamIdentityProvider.cpp` and `InfoHttpHandler.cpp`) shows two **separate** Steam signals:

- `sv_enforceSteamAuth = true` (registered `ConVar_ServerInfo`, so *published* in server info) is a **hard requirement**: `RunAuthentication` rejects any client connection without a Steam auth ticket. This is the automation signal for "Steam required".
- `requestSteamTicket` is a **soft request, not a requirement**: `InfoHttpHandler.cpp` derives it purely from whether `steam_webApiKey` is set (`on`/`off`/`unset`); it means the server *accepts* a ticket if the client offers one. A client without Steam is never rejected on that account. (The earlier spec mislabeled it as "requires a Steam ticket".)

Discord has **no** CFX-published requirement signal at all.

Additionally, players connect to servers by typing an address each time; there is no way to keep servers saved, each with its own manual requirements. The user steered the phase: introduce a **save servers** capability first (name + address, mostly), where each saved server carries its own requirements.

## Solution

The user can save named servers (address + name) and keep them across restarts. Each saved server carries its own manual requirement flags `Steam` / `Discord`. Steam also has an automatic source: when a server publishes `sv_enforceSteamAuth = true`, the launcher treats Steam as required (published wins over the manual flag, per DOC). Before connecting, the launcher checks whether the required apps (Steam, Discord) are *running* and surfaces per-requirement readiness in the status — "Steam ✓ / Discord ✓ / Requires Steam (not running)" — so the player can start missing apps *before* reaching FiveM. The launcher does **not** launch Steam/Discord in this phase, and never manages RSC.

Process detection is the first, coarse readiness state only; the seam is named so a later phase can sharpen "running" into "ready"/"authenticated" per DOC, and a later phase can start missing apps.

## User Stories

1. As a player, I want to save a server under a name with its address, so that I can reuse it without retyping.
2. As a player, I want my saved servers to persist across launcher restarts, so that they are there next time I open the launcher.
3. As a player, I want to see the list of my saved servers, so that I can pick one to connect to.
4. As a player, I want to delete a saved server, so that I keep only the ones I use.
5. As a player, I want to mark a saved server as "requires Steam" / "requires Discord", so that the launcher knows its per-server requirements.
6. As a player, I want the launcher to detect Steam automatically when a server publishes `sv_enforceSteamAuth = true`, so that I don't have to mark it by hand.
7. As a player, I want server-published Steam requirement to win over my manual marking when both exist (per DOC: published requirements win over manual), so that the server's own policy is respected.
8. As a player, I want to connect to a saved server exactly as I connect to a typed address, so that saved servers don't lose any existing capability.
9. As a player, before connecting to a server that requires Steam, I want to know whether Steam is running, so that I don't land in FiveM and get rejected later.
10. As a player, before connecting to a server that requires Discord, I want to know whether Discord is running, so that I don't land in FiveM and get rejected later.
11. As a player, when everything required is running, I want the status to show it as ready (no extra dialogs), so that the "don't bother if everything is ready" philosophy holds.
12. As a player, when a required app is not running, I want the status to say so before the client launches, so that I can start it first.
13. As a player, I want to edit a saved server's name/address/requirements, so that it stays current.
14. As an engineer, I want the readiness check to be a seam, so that "process running" can later evolve into "ready"/"authenticated" without reshaping the flow.
15. As a player, I want the launcher to keep using CFX-provided info wherever possible (not duplicate facts the server publishes), so that manual config stays minimal.

## Implementation Decisions

- **Domain `SavedServer`** (new, pure): `Name`, `Address`, and manual requirement flags `RequiresSteam: bool?`, `RequiresDiscord: bool?` (true = user marked "requires"; unset = no manual opinion). Built through a validated/`Create`-style factory: `Name` non-blank, `Address` classified via the existing `ServerAddress.Classify` (must be a connectable form) — reuses existing domain, no new address parsing.

- **`IServerRepository` seam (Configuration)**: `IReadOnlyList<SavedServer> GetAll()`, `SavedServer? FindByAddress(string)` (case-insensitive match, the "selected saved server" lookup for the connect flow and the list UI), `Add(SavedServer)`, `Update(SavedServer)` (address-keyed, throws when not saved), `Remove(string address)`. Real `FileServerRepository` persists a JSON list to a file, mirroring `FileSettingsStorage` (System.Text.Json, `JsonStringEnumConverter`, create-parent-directory); missing/corrupt file and semantically-invalid entries (null/blank/unconnectable) yield an empty result, never a crash, and `Remove` of an unknown address is a no-op. The file lives beside the launcher settings file (path provided by the composition root). No UI/domain code reaches the file directly.

- **`Core/Enums.ExternalApp`** (new shared enum): `Steam`, `Discord`.

- **`ServerRequirements` extension** (effective values): gains `SteamRequired: bool?` and `DiscordRequired: bool?` (nullable; absent = indeterminate, matching the existing "absent = not published" convention). The resolver produces these as **effective** values: Steam = `sv_enforceSteamAuth` when published else the manual flag; Discord = the manual flag only. `requestSteamTicket` stays exactly as modeled today (it is *not* a "Steam required" signal).

- **`CfxVars`**: reuse the existing strict boolean mapper (`TryGetBool`, `"true"`/`"false"` only) for `sv_enforceSteamAuth` — no new mapping helpers.

- **`ServerResolver`**: maps `sv_enforceSteamAuth` in both the `/single/` path and the catalog path (same `CfxVars` helper), and gains a way to supply the manual overrides (saved server flags). Merge rule lives in the domain (a small resolver/service method or a dedicated effective-requirements resolver) and is directly unit-tested.

- **`IRequirementReadiness` seam (Service)**: `Task<bool> IsRunningAsync(ExternalApp app)`. The real `ProcessReadinessChecker` takes an injectable `Func<string, bool>` process-name check (default: `Process.GetProcessesByName(name).Length > 0`), same ctor style as `ClientInstallLocator`'s `Func<string,bool> exists`. Not found / process check failure → not ready (best-effort, never throws). The seam name is future-proof for "ready"/"authenticated" states.

- **MainViewModel wiring**: the connect flow (a) matches the typed address against a saved server (`FindByAddress`) and passes its manual flags into resolution, typed address otherwise, (b) the resolver merges manual+published requirements into the profile, (c) checks readiness only for the required apps, and (d) surfaces a blocking requirement as "Requires Steam (not running)" (missing apps joined with " / ") instead of launching; when no requirement applies or all required apps are running, nothing extra is shown and the flow launches as today ("Launching FiveM..." / "Opening {client}..." / "Launch failed"). Save/delete feedback uses the same status line ("Server saved" / "Server already saved" / "Invalid name or address"). No business logic in code-behind.

- **Saved-servers UI**: the saved-servers panel lists `SavedServers` (`ObservableCollection<SavedServerItem>`) with per-server "Requires Steam" / "Requires Discord" checkboxes bound to the item; selecting an item fills the connect `ServerAddress`; an add row (name + address) and SAVE/DELETE buttons drive the sync `RelayCommand` wrappers; every mutation goes straight through `IServerRepository` and reloads nothing (the live `ObservableCollection` is the view state, repository is the source of truth). Rename is delete-and-re-add (out of scope otherwise).

- **Minimal first-version UI**: saved-server list + Add/Delete (and rename/edit) + per-server "Requires Steam"/"Requires Discord" checkboxes + the existing address box/connect button/status text. Styling follows the existing minimal `MainView.xaml` and the DOC color palette.

## Testing Decisions

- Good tests assert **external behavior**, not internals: given a saved state, does the repository return it; given published vars + manual flags, what is the effective requirement; given a process-check fake, is the app considered running; given a profile + readiness, what status does the flow show.
- Modules under test and prior art:
  - `SavedServer` factory/validation — prior art: `ServerAddress`/`FiveMLaunchOptions` factory tests.
  - `FileServerRepository` against real temp files (create-when-absent, corrupt file, round-trip) — prior art: `FileSettingsStorage` tests + `TempSettingsDirectory`/`TempCitizenFxIni` fixtures. In-memory fake for higher-layer tests — prior art: `InMemorySettingsStorage`.
  - Effective-requirements merge (published wins over manual) — prior art: `ServerRequirementsResolverTests`.
  - `ProcessReadinessChecker` with injected `Func<string,bool>` — prior art: `ClientInstallLocatorTests` real-impl style.
  - `MainViewModel` connect flow with fake resolver, fake readiness and fake launcher — prior art: `MainViewModelTests`.
- No real processes, no real OS installs, no HTTP in unit tests. Fakes for every seam.

## Out of Scope

- Launching/starting Steam or Discord when required but not running (a dedicated "preparation" phase; the readiness seam and status message prepare for it).
- Beyond "process running" states (ready/authenticated) — the seam name accommodates them but nothing else ships now.
- RSC management (never).
- Per-server manual overrides for game build / pure mode / pool sizes / client choice (only Steam/Discord requirements are manual this phase).
- Cloud sync/import/export/ordering/favorites of saved servers.
- Auto-launch of a specific saved server at startup.

## Further Notes

- This corrects an earlier framing: `requestSteamTicket` was previously described as "the Steam requirement". It is a soft request; `sv_enforceSteamAuth` is the hard "Steam required" signal. Update stale wording in specs/tickets/DOC where it appears.
- `DOC.md` UX section (Steam ✓ / Discord ✓ sequence) is the target behavior; update the "initial UI" section to mention saved servers.
- Commits remain English conventional commits; TDD RED→GREEN→REFACTOR per ticket.
- The four connect-flow address forms and the `fivem://`/launch machinery are untouched by this phase.