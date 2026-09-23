# 04: Catalog endpoint host matching

**What to build:** `ServerCatalog` can match servers by endpoint *host part*. Whole-endpoint
normalization for matching: strip `https://` scheme, trailing slash, and a `:443` port from
URL-form endpoints (e.g. `https://play.foo.io:443/` → `play.foo.io`); plain `ip:port`
endpoints keep their host before the colon. New lookup(s): (a) find server(s) whose
normalized endpoint host equals a given host; (b) exact-match a given `ip:30120` default-port
endpoint. Ambiguity is observable (a host shared by multiple servers must not silently pick
one). Outage semantics unchanged: never throws, no match on error.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

`ServerCatalog.FindByEndpointHostAsync(host)` returns ALL servers whose normalized endpoint
hosts match case-insensitively (ambiguity observable as result count), with internal
`NormalizeEndpointHost` (strip scheme, path/trailing slash, port). Outage → empty, never
throws. Suite green; no refactor needed (helper is small and singular).

- [ ] URL-form endpoints (`https://x:443/`) normalize to bare host for matching
- [ ] Host lookup finds servers publishing a proxy-URL endpoint for that host
- [ ] Host lookup reports ambiguity when several servers share one host
- [ ] Default-port lookup matches `ip:30120` exactly
- [ ] `TestProtobufFrames` extended with URL-form and non-30120 endpoints
- [ ] Outage/corrupt fixtures → no match, no throw
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9c; spike facts)
