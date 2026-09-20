# 03: CFX Catalog service (streamRedir + lookup + TTL cache)

**What to build:** a `CfxService`-style service (seam, no UI state persistence) that downloads the full catalog from `https://frontend.cfx-services.net/api/servers/streamRedir/`, decodes it with the protobuf foundation from ticket 02, and answers questions like "does this `ip:port` appear?" or "does this cfx id exist?" by returning the matching `master.Server` entry. It keeps the stream in memory with a TTL (~5 min) to avoid re-downloading the MBs on every query; if the catalog is unavailable, it returns "no match" (degradation handled by the caller).

**Blocked by:** 02

**Status:** resolved

- [x] Stream fetch via injectable `HttpClient` (testable with `FakeHttpMessageHandler`), with reasonable headers/User-Agent.
- [x] Decoding of all frames using the protobuf helper from 02.
- [x] `LookupByIpPort(ip:port)` matches against `connectEndPoints`; `LookupByEndPoint(id)` matches against `EndPoint`; no match → null.
- [x] The result is exposed to derive `gamename`, `sv_projectName` and the requirement vars (`sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`).
- [x] In-memory TTL cache (~5 min): a second query within the TTL does not re-download.
- [x] With an unreachable/failed catalog → query returns no match (does not throw to the caller).
- [x] Tests with `FakeHttpMessageHandler` and binary fixtures built in the test (frame helper from 02); Given/When/Then style.
- [x] Full suite stays green.

## Comments

- TDD RED→GREEN→REFACTOR completed with 9 new tests in `tests/.../Service/ServerCatalogTests.cs`. Full suite 90 green (81 previous + 9 new).
- `Service/ServerCatalog` (seam, injectable `HttpClient` + optional `TimeProvider` + `cacheTtl`): `LookupByIpPortAsync` (matches `connectEndPoints`) and `LookupByEndPointAsync` (matches `EndPoint`); 5-min TTL cache with injectable `TimeProvider`; network/status/corruption failure → returns no match, does not throw.
- User-Agent `FiveMServerLauncher/0.1` per request (the first version with a `NotNull` assert always passed: `HttpHeaderValueCollection` is never null → assert fixed to `NotEmpty`, real RED).
- New shared test helpers: `TestProtobufFrames` (binary frames, DRY with 02) and `FakeTimeProvider` (manual clock advance for TTL).
- REFACTOR: `FakeHttpMessageHandler` gained `byte[]` support + `RequestCount` + "throws HttpRequestException" mode (simulated network failure).
