# 02: Extract the server-address form into a single place

**What to build:** the "server address" concept (`cfx.re/join/<id>`) now has a single owner in the domain, instead of the same literal duplicated in three places (`ExtractCfxId` in `ServerResolver` and the validation/construction in `FiveMLaunchOptions`). A shared helper/type (DRY) provides: validating whether an address has a server form, deriving the address from a `CfxId`, and extracting the `CfxId` from an address. No observable behavior change.

**Blocked by:** 01 (Harden and seal the factory validation)

**Status:** resolved

- [x] The `cfx.re/join/` literal exists only once in the domain code
- [x] `ServerResolver.ExtractCfxId` uses the shared piece and still extracts the id the same as today (no regression)
- [x] `FiveMLaunchOptions` validation/construction uses the shared piece
- [x] The full suite stays green after the refactor (no new behavior tests)
