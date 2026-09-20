# 02: Protobuf foundation for the catalog (master.Server schema + frames)

**What to build:** the repo learns to decode the CFX catalog's binary format. The `Google.Protobuf` dependency is added (with build-time codegen via `Grpc.Tools`), the `.proto` schema for `master.Server`/`ServerData`/`Player` is defined (verified against the community libraries `cfx-api`/`fivem-server-api`: frame = uint32 LE length + protobuf message) and a helper is delivered that takes the binary stream and returns the sequence of decoded `master.Server` messages. It is the base for ticket 03 and the project's first NuGet dependency.

**Note:** `Player` is **out of scope** (spec.md line 62: "Full catalog detail (icons, upvotes, players) ... only what `ServerResolver` needs is mapped"). The schema only covers `Server` + `ServerData`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Google.Protobuf` + build codegen integrated into the main project (without breaking the build).
- [x] `.proto` schema with `master.Server` (EndPoint + Data) and `ServerData` (vars map string→string, connectEndPoints repeated string, server, clients, svMaxclients, hostname, gametype, mapname...) per what was verified. `Player` out of scope (spec.md:62).
- [x] Parsing helper: reads uint32 LE length prefix per frame, slices the frame and decodes it with the `master.Server` type; corrupt/truncated frames are skipped without dropping the rest.
- [x] A binary fixture frame (built in the test with the protobuf writer) decodes into the expected model (EndPoint and relevant Data fields).
- [x] Existing suite stays green (this ticket changes no domain behavior).
- [x] Naming/tests in Given/When/Then style; binary fixtures built in the test, no loose files.

## Comments

- TDD RED→GREEN→REFACTOR completed with 6 new tests in `tests/.../Service/ServerCatalogDecoderTests.cs`. Full suite 81 green (62 previous + 13 from ticket 01 + 6 new).
- `Google.Protobuf 3.36.2` + `Grpc.Tools 2.84.0` (build-time codegen) added to the main `.csproj`; `Proto/master.proto` registered via `<Protobuf>`. Generated: `obj/.../Proto/Master.cs` (namespace `Master`).
- The decoder (`Service/ServerCatalogDecoder.Decode`) tolerates: truncated frame at the end (bounds-check → stops), corrupt frame between valid ones (catch `InvalidProtocolBufferException` → skip and continue). Explicit LE endianness assumed (Windows-only).
- Generated codegen property: `connectEndPoints` → `ConnectEndPoints`; `svMaxclients` → `SvMaxclients`; `gamename`/`sv_enforceGameBuild`/`sv_pureLevel`/`requestSteamTicket` travel in `vars` (map).
- The schema is "only what the resolver uses" (spec.md:62): no `Player`, no icon/upvote fields.
