# 07: Rename ExtractCfxIdIfValidUrl and clarify the EndPoint→CfxId mapping

**What to build:** the `ExtractCfxIdIfValidUrl` method of `ServerResolver` validates nothing: it only extracts the substring from `cfx.re/join/`. It is renamed to say what it does (`ExtractCfxId`). Also the mapping in `CfxService.cs` that assigns `EndPoint` to `CfxId` is clarified (fixtures use a URL as EndPoint and the resolver returns it as the id).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] The method is renamed to a name that reflects its real behavior
- [x] Existing tests stay green
- [x] It is documented/clarified why `EndPoint` feeds `CfxId` (or the mapping is fixed)
- [x] No behavior changes
