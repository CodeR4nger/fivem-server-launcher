# 01: ServerProfile preserves GameClient

**What to build:** the `ResolveAsync` composition must not lose the `GameClient` (FiveM | FiveMEnhanced | RedM) that CFX publishes in `gamename`. The `ServerProfile` returned by the resolver exposes it so later layers (e.g. `Requirements`) can query it.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `ServerProfile` exposes a `GameClient` field
- [x] Resolving a server with `gamename: gta5enhanced` returns the profile with that mapped GameClient
- [x] A server without `gamename` leaves the field at `null` (do not assume a client)
- [x] Test covers the mapping in `ServerResolver` (high-level seam)
