Status: resolved
Type: spec

# Resolution of IP:port and domain:port addresses in ServerResolver (with validation)

## Problem Statement

Today `ServerResolver.ResolveAsync` only supports a CFX address (`cfx.re/join/<id>`, with or without scheme, or a bare `cfxId`). The TODO in the code (`//TODO: Implement IP and domain filter`) and DOC.md (Address section, lines 111-127) require also supporting `IP:port` and `domain:port` (e.g. `149.56.120.52:30320`, `play.example.com:30120`). Also, the current extraction (`ServerAddress.ExtractCfxId`) does not validate the address form (explicit TODO in AGENTS.md), and `HasServerFormWithNonEmptyId` only covers the `cfx.re/join/` form. DOC.md:530-534 requires researching before assuming technical decisions; this spec incorporates what was researched about how CFX identifies servers by endpoint.

## Solution

`ServerResolver.ResolveAsync` accepts the **four forms** from DOC.md (CFX ID, CFX URL, IP:port, domain:port) and returns a `ServerProfile` or throws `InvalidAddressException`. Address classification and validation live in `ServerAddress` (already the owner of the CFX form; extended as the single source). For `IP:port`/`domain:port` the launcher tries to resolve the server through CFX; if the server **is not published/validated on CFX**, it still connects directly but the `ServerProfile` is marked as **unvalidated** so the UI (in a later phase) can show a warning, without blocking the connection.

## User Stories

1. As a player, I want to enter a bare `CFX ID` (`8e8xxv`) and have it resolve as today, so the existing flow isn't broken.
2. As a player, I want to enter a `cfx.re/join/<id>` URL (with or without scheme) and have it resolve as today.
3. As a player, I want to enter `IP:port` (`149.56.120.52:30320`) and have the launcher resolve it to a server profile, so I can connect to servers not reachable via CFX.
4. As a player, I want to enter `domain:port` (`play.example.com:30120`) and have it resolve the same as IP:port, so I don't depend on remembering the IP.
5. As a player, I want to still be able to connect when the `IP:port`/`domain:port` **is not published on CFX** (profile with direct address), to join private or unlisted servers.
6. As a player, I want a server not published on CFX to be **marked as unvalidated** (but connectable), so the UI can warn me later without preventing the connection.
7. As a player, I want an invalid address (empty, broken format, out-of-range port, malformed `IP`) to throw `InvalidAddressException`, to fail fast and clearly.
8. As a developer, I want address classification/validation to be pure testable code in `ServerAddress` (no HTTP, no filesystem), to cover all forms with fast unit tests.
9. As a developer, I want not to break the existing `ServerResolver`/`ServerAddress` tests (CFX-only), keeping compatibility with the CFX form.

## Implementation Decisions

- **`ServerAddress` is extended as the single source of address form.** It adds (a) address classification into an enum (`CfxId`, `CfxJoinUrl`, `IpPort`, `DomainPort`, unknown/invalid) and (b) per-form validation/extraction. `ExtractCfxId` and `HasServerFormWithNonEmptyId` keep existing and are re-expressed on top of the new classifier (without breaking existing call-sites).
- **`ServerResolver.ResolveAsync` branches by form:**
  - `CfxId` / `CfxJoinUrl` → current behavior (look up in CFX by id; `null` → `InvalidAddressException`).
  - `IpPort` / `DomainPort` → try resolving via CFX; if resolution returns a profile → validated profile; if not → **connectable unvalidated profile** with the address as-is, without derived requirements (do not invent `ProjectName`/`GameClient`/`Requirements`).
  - Unknown/invalid form → `InvalidAddressException`.
- **`ServerProfile` gains a validation marker** (e.g. `bool IsCfxValidated`) to distinguish "full profile from CFX" from "direct unvalidated connectable profile". In this phase it is used as resolver output; UI consumption (warning) is another phase.
- **IP/domain lookup mechanism on CFX (researched and verified):**
  - There is no lightweight endpoint lookup. The full catalog is served at `https://frontend.cfx-services.net/api/servers/streamRedir/` as a **binary frame stream** (uint32 LE length prefix + protobuf `master.Server` message), exceeding 5MB and **ignoring query params** (no server-side pagination/filters: `?limit=` still returns everything). Filters are 100% client-side.
  - **`master.Server`** (`EndPoint` + `ServerData`) exposes `vars` (string→string map, incl. `gamename`, `sv_projectName`, `sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`), `connectEndPoints` (IP:port or host with `sv_listingHostOverride`), `server`, `clients`, `svMaxclients`... (full schema verified in `cfx-api`/`fivem-server-api`).
  - Looking up an endpoint = **downloading the full stream, decoding all frames, and matching** `connectEndPoints` against `ip:port` (or against the resolved host) or `EndPoint` against a cfx id. It is what both community libraries do.
  - **Protobuf decode with `Google.Protobuf`** (decision confirmed with the user): repo-owned `.proto` schema based on the verified one; binary test fixtures.
  - **TTL cache** (e.g. 5 min) to avoid re-downloading the MBs on every resolution: the catalog download is expensive and the server set changes slowly.
  - **`domain:port`**: resolve DNS → IP, and match against `connectEndPoints` (IP:port or published host).
- The resolver **tries** this path and degrades to "connectable unvalidated" without throwing if the endpoint doesn't appear in the catalog (or the catalog is unavailable).
- **Strict minimal validation:**
  - `IP:port` → 4 decimal octets (0-255) separated by `.` + `:` + port (1-65535).
  - `domain:port` → 1+ alphanumeric labels with `-` joined by `.` (no scheme, no path, no user@) + `:` + port (1-65535).
  - No whitespace in any format.
- **Domain/IP and the `gamename`/requirements detail:** if the form is IP/domain and CFX returns no profile, no requirements are derived (YAGNI: don't guess build/pure/steam from a raw address).
- **`ServerAddress` remains the only place knowing the `cfx.re/join/<id>` form** (`Domain/ServerAddress` is already the "single owner" per AGENTS.md); the new classification lives there.
- In this phase **there is no UI**: the "unvalidated server" warning is just a profile state ready for future consumption.

## Testing Decisions

- **Modules to test:** `ServerAddress` (classification + validation/extraction across the 4 forms and invalid cases), `ServerResolver` (branching by form: CFX resolves, IP/domain with CFX profile returned, IP/domain without CFX profile → connectable unvalidated, invalid form → exception) and the **catalog service** (stream fetch + frame decode + matching by `connectEndPoints`/`EndPoint`, with binary protobuf fixtures).
- **Prior art in the repo:** `tests/.../Domain/ServerResolverTests.cs` (fake HTTP with `FakeHttpMessageHandler`, Given/When/Then, `CreateResolver` helper) and `tests/.../Domain/ServerAddressTests.cs` (pure, no HTTP). New tests follow that pattern, with no mocking libraries.
- The HTTP response is faked with `FakeHttpMessageHandler` and JSON fixtures as C# raw strings (repo convention); for the binary catalog, fixtures are sequences of protobuf bytes/frames generated in the test (frame-building helper).
- Naming: `<Method>_Should<Expectation>` with Given/When/Then comments.
- The "connectable unvalidated" state is verified via the new `ServerProfile` field (observable resolver behavior), not via internal details of how CFX was attempted.

## Out of Scope

- **UI/visual warning** for "unvalidated server": later phase consuming `IsCfxValidated`.
- **On-disk catalog cache** and advanced refresh policies (in-memory TTL only for now; persistence/background refresh is a future iteration).
- **Full catalog detail** (icons, upvotes, players): only what `ServerResolver` needs is mapped (`gamename`, requirements, endpoints).
- **`/api/servers/top/{language}`** and other catalog endpoints: not needed for endpoint lookup.
- **IPv6 (`[::1]:30120`)** and other exotic formats: not requested by DOC.md.
- **Steam/Discord** as manual profile requirements (still out).
- **Modifying `LauncherSettings`** or any global config.
- **Noisy `ServerAddress` refactor**: the new classifier must coexist with current APIs without changing existing signatures except where behavior demands it (and it is tested).

## Further Notes

- DOC.md:516-517, 530-534: IP/domain resolution and the lookup mechanism depend on current CFX info → research before implementing the mechanism; **research done and reflected in Implementation Decisions** (streamRedir, protobuf with Google.Protobuf, TTL cache).
- The community libraries `fivem-server-api` and `cfx-api` (both maintained) already implement the "full stream + local filter" pattern; they are the behavior reference for the catalog service (identical `master.Server` schema in both).
- DOC.md:127-128 ("When possible, a CFX address must be resolvable via the CFX API") reinforces the "try CFX → degrade to connectable" design.
- DOC.md:514 ("Don't assume process started = ready") and 434-435 ("don't assume requirements") support not inventing requirements/GameClient in the unvalidated profile.
- Nullable `GameClient` in `ServerProfile` already covers "not published"; the unvalidated profile simply leaves it `null`.
- After implementing: update `AGENTS.md` (current state, remove IP/domain and validation TODOs) and commit with English conventional commits.
