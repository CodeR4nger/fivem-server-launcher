# 05: Resolver support for port-less forms + proxied domains

**What to build:** `ServerResolver` resolves the new port-less kinds and improves
`domain:port`:

- Bare domain: (a) unique hostname string-match against catalog endpoint host parts →
  validated profile; (b) else DNS-resolve and exact-match `resolvedIP:30120` → validated;
  (c) else unvalidated connectable profile with the address used as-is.
- Bare IP: exact-match `ip:30120` → validated; else unvalidated connectable profile as-is.
- `domain:port`: hostname string-match attempted alongside the existing DNS→`ip:port` and
  raw-string matches.
- Ambiguous hostname (multiple servers share the proxy host) → no invented enrichment:
  unvalidated connectable fallback.
- DNS failure or catalog outage still degrades to unvalidated, never throws.

**Blocked by:** 03 (Port-less address classification), 04 (Catalog endpoint host matching).

**Status:** resolved

## Answer

`ServerResolver` handles the new kinds: bare IP → exact `ip:30120` catalog match else
unvalidated as-is; bare domain → unique hostname match (no DNS call) → DNS+`:30120` →
unvalidated as-is; ambiguous shared proxy host → unvalidated. `domain:port` now also falls
through to unique hostname matching when DNS/raw lookups miss (catches proxy-URL endpoints).
Suite 457 green.

- [ ] Bare domain with unique catalog hostname match → validated profile (CfxId, game, etc.)
- [ ] Bare domain with no hostname match but DNS+default-port match → validated profile
- [ ] Bare domain with no match → unvalidated connectable profile, address as-is, no invented fields
- [ ] Bare IP: ip:30120 match → validated; no match → unvalidated as-is
- [ ] `domain:port` matches proxy-URL endpoints by hostname (validated)
- [ ] Ambiguous shared proxy hostname → unvalidated fallback (no enrichment invention)
- [ ] DNS failure / catalog outage → unvalidated, never throws
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9c)
