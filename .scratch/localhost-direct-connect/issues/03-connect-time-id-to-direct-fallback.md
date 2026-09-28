# 03: Connect-time id-first, direct-fallback resolution

**What to build:** When the user connects via a cfx id / join URL whose server the CFX service
cannot resolve (delisted during restart, CFX outage) and a saved server links that id to a direct
address, the launcher falls back to a direct connect using that saved address instead of failing
with "Invalid address". The fallback profile is unvalidated, carries the saved server's raw
address, and merges the saved server's manual Steam/Discord flags. Without a saved direct
address the failure stays as today. Normal resolution is unchanged.

**Blocked by:** 02 (find saved server by cfx id).

**Status:** ready-for-agent

- [ ] Resolver: id-form resolution failure + passed saved server with a direct-form address
      yields an unvalidated profile with that address (validated resolution unchanged)
- [ ] Resolver: id-form failure with no direct address available still raises the invalid-address
      failure
- [ ] Resolver: the fallback profile's effective requirements merge the saved server's manual
      flags (additive Steam rule included)
- [ ] Connect flow: saved context is matched by typed address first, else by cfx id when the
      typed address is a cfx form
- [ ] End to end: saved direct server + its id typed while the CFX service is unavailable still
      launches a direct connect to the saved address
- [ ] End to end: an unknown id with no saved row still shows the invalid-address status
