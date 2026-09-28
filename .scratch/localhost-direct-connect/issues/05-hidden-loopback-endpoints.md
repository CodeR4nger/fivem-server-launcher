# 05: Hidden servers with loopback endpoints must never match by address

**What to build:** Publicly listed "hidden" servers publish non-connectable endpoints — the
`private-placeholder.cfx.re` sentinel but also raw loopback endpoints such as `127.0.0.1:30120`
(e.g. cfx id `8y6354`). Today a typed `localhost`/`127.0.0.1` address that reaches catalog
matching validates against such a stranger's server and connects via its cfx id, and a browser
row for a hidden server exposes its loopback endpoint as the row address. Hidden servers must be
reachable by cfx id only, never by address: catalog address matching ignores hidden endpoints,
and browser rows for hidden endpoints show the join form (so saving them keeps the id form).

**Blocked by:** None (bug fix on feature 1's matching rules; supersedes the "listed loopback
validates localhost" assumption in tickets 01/04).

**Status:** done

- [x] Catalog ip:port / bare-ip / bare-domain and endpoint-host matching ignore hidden endpoints
      (sentinel host, loopback host `127.0.0.1`, `localhost`), so typed local addresses never
      resolve to a hidden stranger's server
- [x] A server publishing both a public and a hidden endpoint still matches via its public
      endpoint
- [x] Browser rows whose (first) endpoint is hidden show the cfx join form as address, keeping
      browser-saved hidden servers connectable by id
- [x] Resolver/enrichment localhost behavior updated: a loopback catalog listing never validates
      or captures an id for typed localhost/loopback addresses (unvalidated direct connect,
      plain rows)
- [x] Domain glossary updated: hidden server covers loopback endpoints, not only the sentinel
