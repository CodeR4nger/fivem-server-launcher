# 01: SavedServer.CfxId + repository round-trip

**What to build:** optional `CfxId` (`string?`) on `SavedServer` (ctor/`Create`/`[JsonConstructor]`),
persisted through the address-keyed repository; old files without the field deserialize to null;
`Create` captures the id directly for CFX-form addresses (bare id / `cfx.re/join/...` via
`ServerAddress.ExtractCfxId`). Repository identity (address) unchanged; `Update` supports
re-saving an enriched CfxId.

**Blocked by:** none.

**Status:** ready-for-agent

- [ ] `SavedServer` carries optional `CfxId`; JSON round-trips, null when absent.
- [ ] CFX-form `Create` stores the extracted id; IP/port and domain forms leave it null.
- [ ] Repository `Update` persists CfxId without changing identity/duplicate semantics.