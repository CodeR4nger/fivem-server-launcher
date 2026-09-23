# 06: Launch & save integration for port-less addresses

**What to build:** port-less addresses work end-to-end. `FiveMLaunchOptions` accepts the new
kinds: validated profiles serialize by cfx id as today; unvalidated port-less addresses go
into the connect URI as-is (no port — FiveM defaults 30120). `SavedServer.Create` accepts the
new kinds so they can be saved and edited. Background CfxId capture
(`ServerEnrichmentService.ResolveCfxIdAsync`) supports port-less forms using the same
matching rules as the resolver (hostname match, then DNS+default-port for bare domains;
`ip:30120` for bare IPs), so saved port-less rows get enriched like any other.

**Blocked by:** 05 (Resolver support for port-less forms + proxied domains).

**Status:** resolved

## Answer

`FiveMLaunchOptions.Create` accepts `IpAddress`/`DomainName` (URI emits address as-is, no
port). `SavedServer.Create` accepts them via the existing Unknown-rejection (characterization
tests added, CfxId null at create). `ServerEnrichmentService.ResolveCfxIdAsync` handles the
new kinds with the resolver's matching rules (unique host match → DNS+30120; bare IP →
`ip:30120`; ambiguous host → null). Suite green.

- [ ] `FiveMLaunchOptions`/`ToUri()` handle validated (cfx id) and unvalidated (as-is,
      port-less) profiles from the new kinds
- [ ] `SavedServer.Create` accepts bare IP and bare domain; rejects Unknown as before
- [ ] Saving a port-less address captures its CfxId in the background when the catalog
      matches (hostname / DNS+30120 / ip:30120)
- [ ] Existing save/edit behaviour unchanged for the four old forms
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9c)
