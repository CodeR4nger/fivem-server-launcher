# 03: Port-less address classification

**What to build:** `ServerAddress.Classify` accepts two new connectable kinds — a bare IPv4
address with no port (e.g. `85.12.34.56`) and a bare domain with no port (e.g.
`play.example.com`) — instead of classifying them `Unknown`. Validation reuses the existing
octet/label rules, minus the port requirement. A single default-port (30120) constant is
defined in the domain layer. Existing forms (cfx id precedence, cfx.re/join, ip:port,
domain:port) classify exactly as before; whitespace/invalid hosts still land `Unknown`.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

Implemented TDD in `Domain/ServerAddress.cs`: new kinds `IpAddress` (bare IPv4) and
`DomainName` (bare domain, requires a dot so single labels still land `CfxId`), `DefaultPort
= 30120` constant; classification refactored to a single host/port split (DRY — dead
`IsIpPort`/`IsDomainPort` helpers removed). Suite 444 green.

- [ ] Bare IPv4 without port classifies as the new IP (port-less) kind
- [ ] Bare domain without port classifies as the new Domain (port-less) kind
- [ ] Existing four forms classify unchanged (regression-safe)
- [ ] Malformed hosts (bad octets, bad labels, whitespace, trailing dot weirdness) stay Unknown
- [ ] Default port 30120 defined once in the domain layer
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9c)
