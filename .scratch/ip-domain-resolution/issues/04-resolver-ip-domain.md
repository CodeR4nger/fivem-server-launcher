# 04: Resolve IP/domain in ServerResolver with degradation to unvalidated

**What to build:** `ServerResolver.ResolveAsync` accepts the classifier's 4 forms (ticket 01). For `CfxId`/`CfxJoinUrl` it keeps the current flow. For `IpPort`/`DomainPort` it uses the catalog service (ticket 03): if the endpoint appears published, it returns a **validated** `ServerProfile` with the catalog data (incl. requirements and gamename); if it doesn't appear (or domain DNS fails), it returns a **connectable unvalidated profile** with the address as-is, without inventing requirements, marked with the new `IsCfxValidated = false`. Invalid forms (classifier) throw `InvalidAddressException`. The domain's DNS is resolved to IP for matching against `connectEndPoints` via an injectable seam (real + fake in tests).

**Blocked by:** 01, 03

**Status:** resolved

- [x] `ServerProfile` gains `IsCfxValidated` (true for profiles resolved from CFX/catalog; false for degraded direct connectable).
- [x] `CfxId`/`CfxJoinUrl` → current flow intact (CFX lookup by id; null → `InvalidAddressException`).
- [x] `IpPort` found in catalog → validated profile with catalog data (requirements + gamename when the catalog publishes them).
- [x] `IpPort` not published → connectable profile `IsCfxValidated=false`, `CfxId`/`ProjectName`/`GameClient`/`Requirements` with no invented data.
- [x] `DomainPort` → DNS to IP (seam), matching by IP:port in catalog; failed DNS → connectable unvalidated profile (no exception).
- [x] Invalid/unknown forms from the classifier → `InvalidAddressException`.
- [x] Without an available catalog (network failure) on an IP/domain → connectable unvalidated profile (degradation, not an exception).
- [x] Tests in `ServerResolverTests` with fakes (HTTP + DNS), Given/When/Then style; `CreateResolver` updated; full suite green.

## Comments

- TDD RED→GREEN→REFACTOR completed with 7 new tests in `tests/.../Domain/ServerResolverTests.cs` (the previous 10 stay green). Full suite 97 green (90 previous + 7 new).
- `ServerResolver` now injects `ServerCatalog` + `IDnsResolver` (new DNS seam in `Domain`, `FakeDnsResolver` fake in tests). `ResolveAsync` branches by `ServerAddressKind`:
  - `CfxId`/`CfxJoinUrl` → existing CFX flow + `IsCfxValidated=true`.
  - `IpPort` → `LookupByIpPortAsync`; found → validated; not → connectable unvalidated with the raw address.
  - `DomainPort` → DNS host→IP; `LookupByIpPortAsync` by `ip:port` and then by the raw host; failed DNS or no match → unvalidated (no exception).
  - `Unknown` → `InvalidAddressException`. Whitespace and empty too.
- Unvalidated profile: `Address` = raw address, everything else empty/null. Validated profile from catalog: `CfxId`=EndPoint, `Address`=first connectEndPoints, ProjectName=`sv_projectName`→hostname, GameClient/Requirements from vars.
- `ServerProfile` gained `Address` (for the connect-directly flow) and `IsCfxValidated`.
- REFACTOR (DRY): vars mapping extracted into `Service/CfxVars` shared by `CfxService` and `ServerResolver` (gamename→GameClient, int, steam ticket); `CfxService` got slimmer.
- Fix along the way: `LookupByIpPortAsync` was null-unsafe with missing `Data` in catalog entries without `Data` (NRE in the predicate) → null-safe.
