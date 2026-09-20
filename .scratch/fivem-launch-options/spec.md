Status: ready-for-agent
Type: spec

# FiveMLaunchOptions — preparing the launch from server requirements

## Problem Statement

`ServerProfile` already resolves a server and derives its `Requirements` (Game Build, Pure Mode, Steam ticket) from what CFX publishes. The launcher still cannot translate that information into what FiveM needs at launch: which command-line args to pass (`-b`, `-pure_`, `-cl2`) or which connection URI to open (`fivem://connect/<addr>`). Today there is no representation of those launch options, so the layer connecting requirements to FiveM execution cannot be tested or implemented in a controlled way.

## Solution

`FiveMLaunchOptions` appears: an immutable domain value that describes how to launch FiveM for a given intent (connect to a server or open the client directly). It is built from a `ServerProfile` (connecting) or from preferences/Dev Mode (opening directly), and knows how to serialize itself to a `fivem://` URI or to a list of command-line arguments. It launches no processes and touches no files: it only models and serializes, so the future launch layer consumes something already proven.

## User Stories

1. As a player, I want the launcher to generate a `fivem://connect/<addr>` URI from the `ServerProfile` when connecting to a server, so the client opens pointing directly at the server.
2. As a player, I want the URI to include the server's `GameBuild` (`-b<build>`) when CFX publishes it, to boot into the correct build and avoid the cross-build restart.
3. As a player, I want the URI to include the server's `PureMode` (`-pure_<level>`) when CFX publishes it, to boot into the required pure mode.
4. As a player, I want the URI to **not** include args for absent (`null`) requirements, so it doesn't assume values the server doesn't publish.
5. As a player, I want command-line args (`-b`, `-pure_`, `-cl2`) serialized for opening FiveM directly (Dev Mode / open client), independent of the connection URI.
6. As a player, I want the serialization to distinguish **connecting** (address present → URI) from **opening directly** (no address → args), preserving DOC.md's connect-vs-open separation.
7. As a player, I want `-cl2` (second instance) to appear only when the user explicitly asks (Dev Mode), so an extra instance is not always launched.
8. As a **FiveM Enhanced** player, I want the launcher **not** to generate a `fivem://connect` URI nor `-b`/`-pure_`/`-cl2` args, since that client doesn't support them (verified), and connecting happens inside its own UI.
9. As a player, I want the serialization to return an empty URI/args with no extras when there are no requirements and no Dev Mode flags, to reflect "launch normally, modify nothing".
10. As a developer, I want `FiveMLaunchOptions` to depend only on domain types (`ServerProfile`, `ServerRequirements`, `GameClient`) and not on UI or services, so I can test it without processes or filesystem.
11. As a developer, I want the model to validate its state (e.g. malformed address or invalid GameClient) at construction, to avoid silently serializing impossible launches.
12. As a developer, I want the source of `GameBuild`/`PureMode` when connecting to always be `ServerRequirements` (CFX-published), so server requirements win over any manual ones.
13. As a developer, I want a `FiveMLaunchOptions` constructed directly with few fields to still be serializable, to serve tests and simple cases (opening the client without a server).

## Implementation Decisions

- **Single seam (confirmed with the user): `FiveMLaunchOptions` module** in `Domain`. An immutable value with a static factory and two serializations (`ToUri()` and `ToCommandLineArgs()`), tested directly. No Service or Configuration seams are created for this phase.
- **Model shape** (rich decisions from a prototype; trimmed to the essentials):
```csharp
public sealed record FiveMLaunchOptions
{
    public string? Address;        // cfx.re/join/<cfxId> when connecting; null when opening directly
    public GameClient? GameClient; // FiveM | FiveMEnhanced | RedM; from ServerProfile or PreferredClient
    public int? GameBuild;         // -b<build>
    public int? PureMode;          // -pure_<level>
    public bool SecondClient;      // -cl2 (Dev Mode)

    public Uri? ToUri();                        // null if Address or GameClient is FiveMEnhanced
    public IReadOnlyList<string> ToCommandLineArgs();
}
```
- **`ToUri()`**: `fivem://connect/<address>`; adds `?-b<build>` and `?-pure_<level>` for present requirements (parameter format with `?` as separator, verified in docs/code: `fivem://connect/<server>?<params>`). Returns `null` when there is no `Address` or when `GameClient` is `FiveMEnhanced`.
- **`ToCommandLineArgs()`**: list of args to open the client directly: `-b<build>`, `-pure_<level>`, `-cl2` (only if `SecondClient`). Does **not** include a server address. For `FiveMEnhanced` returns an empty list (no `-b`/`-pure_`/`-cl2`, verified).
- **`GameClient` is included** in the model (confirmed with the user) to be able to deny serialization incompatible with Enhanced. When connecting it comes from `ServerProfile.GameClient`; when opening directly the **caller decides** it (e.g. the UI, reading `LauncherSettings.PreferredClient` at its level, and passing it to the model). This phase introduces no factories or code that read `LauncherSettings`.
- **Construction**: a static factory that validates state (malformed address, invalid `GameClient`) and a constructor/record accepting the fields; requirements are copied from `ServerRequirements` with absent nulls. No IO or processes.
- **Connect vs open**: two intents of the same model, distinguished by the presence of `Address`: with `Address` use `ToUri()`; without `Address`, `ToCommandLineArgs()`. Matches the already-agreed dual use of the shape.
- **Vocabulary per DOC.md**: `FiveMLaunchOptions`, `ServerProfile`, `Requirements`, `GameClient`. `LauncherSettings` is untouched; no per-server fields are added.
- DOC.md constraints respected: the launcher never manages RSC, Steam/Discord do not enter here, server requirements win when connecting, and Dev Mode applies only when opening directly.

## Testing Decisions

- A good test verifies **observable external behavior**: given a known state, `ToUri()` returns the expected URI (or `null` for Enhanced / no address) and `ToCommandLineArgs()` returns the expected args (or empty). It never exposes implementation details or does IO.
- **Module to test:** `FiveMLaunchOptions` (single seam, directly testable with no HTTP or filesystem). No Service seams involved in this phase.
- **Prior art in the repo:** `tests/.../Domain/ServerRequirementsResolverTests.cs` (direct value mapping, Given/When/Then style, absent nulls) and `tests/.../Domain/ServerResolverTests.cs`. New tests follow that pattern; cases per intent: connect (URI) and open directly (args), plus the Enhanced case (negation).
- Naming: `<Method>_Should<Expectation>` with Given/When/Then comments (repo convention).

## Out of Scope

- **Launching processes**: `Process.Start`/running FiveM, `IProcessRunner` or equivalents. This spec only models and serializes.
- **`fivem://connect` toward Enhanced**: verified not applicable; only the client is opened.
- **IP:port / domain:port resolution** (open `ServerResolver` TODO).
- **CitizenFX.ini**, pool sizes, Steam/Discord, RSC.
- **UI/MVVM**: `MainView.xaml` stays static and unbound.
- **Steam ticket**: `RequestSteamTicket` does not participate in launch serialization (it's Steam preparation, another phase).
- **`CfxId` validation** (open TODO): this model validates its own state, not that of `ServerResolver`'s input.

## Further Notes

- DOC.md:523: "If a technical decision depends on current FiveM/CFX information, research it before assuming." This spec incorporates verified research on CLI args, `PureModeState.h`, `CrossBuildSwitch.cpp`, `CitizenFX.ini` and the FiveM Enhanced state (no `-cl2`, no `+set moo`, pure mode always on, latest gamebuild only, redesigned restart-free connection).
- `ServerResolver` still exposes `ExtractCfxId` (`cfx.re/join/` prefix); the connection address for the URI derives from the `ServerProfile`'s `CfxId`, not from `EndPoint` (removed in the previous phase).
- After implementing: update `AGENTS.md`, check boxes as `[x]` and commit with English conventional commits.
