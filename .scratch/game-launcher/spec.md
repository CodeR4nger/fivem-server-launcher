Status: resolved
Type: spec

# GameLauncher: launch a server connection from a ServerProfile

## Problem Statement

The domain can already summarize what to do for a server (`ServerProfile` → `FiveMLaunchOptions` → `fivem://` URI or CLI args), but nothing orchestrates the real launch. The (future) UI needs an `Application/Manager` layer that, given an already-resolved `ServerProfile`, decides *what* to launch and delegates execution to the real process.

The two possible outcomes of the decision (verified against official docs and citizenfx/fivem code):

- **Connectable client (Legacy/RedM with URI):** `fivem://connect/<addr>?-b<build>?-pure_<level>` is the direct-connection mechanism (protocol registered by the client itself; the official forum confirms `fivem://connect/<server>`, and `connect 127.0.0.1:30120` and `connect cfx.re/join/y4lg95` work).
- **`FiveMEnhanced`:** does not support `fivem://connect`, `-b`, `-pure_` or `-cl2` (DOC.md). The launcher can only *open the client*; connecting happens inside its own UI.

There is also a case `FiveMLaunchOptions.FromServerProfile` doesn't cover today: an **unvalidated** profile (`IsCfxValidated=false`, e.g. an IP:port not published on CFX) has an empty `CfxId` and a raw `Address` (`149.56.120.52:30320`). `FromServerProfile` forces `FromCfxId(CfxId)` → empty `cfx.re/join/` → `InvalidAddressException`. The GameLauncher must be able to connect to that direct server by its `Address`.

## Solution

New `Application` layer (a single orchestrator class + a process seam):

- `GameLauncher.ConnectAsync(ServerProfile)` → returns a launch result (`LaunchResult`):
  - `Connect(Uri)` when the profile serializes a `fivem://connect/...` URI (Legacy/RedM) — after delegating execution to the seam.
  - `OpenClient(GameClient)` for `FiveMEnhanced` (or unaddressed client) — does **not** call the process seam: it is the UI that will open the client afterwards.
  - Builds the options from the profile using `CfxId` if present (join form) or the direct `Address` otherwise (IP:port/domain).
- **New seam:** `IGameProcessLauncher` with a single method (e.g. `Task StartAsync(Uri uri)`). Production: `Process.Start`/ShellExecute of the `fivem://` URI (FiveM handles the protocol). Tests: fake that records the `Request`s without real processes.
- `FiveMLaunchOptions` is extended (decision recorded below) so that `Address` can be a valid non-join server address (IP:port/domain), preserving the existing invariants (creation only via `Create`/factories, immutable, null-guard, reusing `ServerAddress`).

### Decision: `FiveMLaunchOptions.Create` accepts IP:port/domain

This partially contradicts the invariant from the `launch-options-invariant-review` phase (which only admitted `cfx.re/join/`). Rationale: a server not published on CFX connects directly by IP:port and the `fivem://connect/149.56.120.52:30320` URI is the official mechanism. Validation moves from `HasServerFormWithNonEmptyId` to "null **or** any `ServerAddress.Classify` in `CfxJoinUrl|IpPort|DomainPort`" (the classification class already centralizes this). Documented in AGENTS.md.

## User Stories

1. As a player, connecting to a Legacy/RedM server published on CFX must launch `fivem://connect/cfx.re/join/<id>?-b<build>?-pure_<level>` via the process seam.
2. As a player, connecting to a `FiveMEnhanced` server does not launch a URI; the launcher returns `OpenClient(FiveMEnhanced)` and the UI will open the client afterwards.
3. As a player, connecting to an unpublished server (IP:port/domain) must launch `fivem://connect/<ip:port>` directly.
4. As a developer, I want the process seam to be a single (fakeable) point to test the orchestrator without real processes.

## Implementation Decisions

- `LaunchResult`: immutable type (record or sealed) with `Kind` (`Connect | OpenClient`) and payload (`Uri?` or `GameClient?`). Explicit factories; no public ctor.
- `GameLauncher` receives `IGameProcessLauncher` via ctor. `ConnectAsync`:
  - Builds `FiveMLaunchOptions` from the profile: non-empty `CfxId` → `FromCfxId`; if empty and `Address` classifies as IpPort/DomainPort → direct `Address`. `GameClient`/`Requirements` from the profile.
  - `ToUri()` null → `LaunchResult.OpenClient(GameClient ?? PreferredClient fallback from the profile)`. Does not throw, does not call the seam.
  - URI present → calls the seam and returns `LaunchResult.Connect(uri)`.
- `FiveMLaunchOptions.Create`: `ValidateAddress` accepts `null` or classification `CfxJoinUrl | IpPort | DomainPort` (via `ServerAddress.Classify`). `FromServerProfile` picks `CfxId`/`Address` based on the profile. Existing invalid-`IpPort` tests still hold: an `Address` that doesn't classify still throws.
- This case reuses `ServerAddress` (owner of the classification), no duplicated regex.
- `IGameProcessLauncher` in `Application`. Fake in tests (`FakeGameProcessLauncher`) that records received URIs and exposes `RequestCount`.
- Out of scope: PathResolver/install detection, Dev Mode, `-cl2`, Steam/Discord, status checks, splash/progress, real seam implementation with `Process.Start` (the contract is left; the UI will wire it).

## Testing Decisions

- TDD at the `GameLauncher` seam (only the seam confirmed with the user: `GameLauncher.ConnectAsync` → `LaunchResult`, with a fake `IGameProcessLauncher`).
- Tests per vertical slice:
  1. Legacy profile with CfxId + requirements → seam receives `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1`; result `Connect`.
  2. `FiveMEnhanced` profile → result `OpenClient`, seam NOT called.
  3. Unvalidated profile with IP:port `Address` → seam receives `fivem://connect/149.56.120.52:30320`; result `Connect`.
  4. `FiveMLaunchOptions` regression: `Create` with valid IP:port/domain does not throw; with a garbage string it still throws `InvalidAddressException`.
- Naming `<Method>_Should<Expectation>` with Given/When/Then.

## Out of Scope

- Real `IGameProcessLauncher` implementation with `Process`/ShellExecute.
- PathResolver / FiveM/Enhanced install detection.
- Dev Mode, `-cl2`, Steam/Discord requirements, status checks.
- MVVM / UI wiring.

## Further Notes

- Verified mechanism: `fivem://connect/<server>` protocol (used by the client itself), Cfx.re forum and fivem.net docs confirm direct connection by IP:port and cfx.re/join. `connect 127.0.0.1:30120` works via URI.
- `DOC.md` places `GameLauncher` in the Application layer; keep logic out of XAML and real processes.
- On closing: update AGENTS.md (Application layer, `IGameProcessLauncher` seam, `FiveMLaunchOptions.Create` accepting IpPort/DomainPort), English conventional commit.
