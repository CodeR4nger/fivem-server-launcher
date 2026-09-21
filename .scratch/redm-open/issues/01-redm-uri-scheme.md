# 01: ToUri scheme per client (redm:// for RedM)

**What to build:** `FiveMLaunchOptions.ToUri()` emits `redm://connect/<addr>` when the profile's
GameClient is RedM, keeping `fivem://connect/<addr>` otherwise. Enhanced never gets a URI. Tests
cover all three clients.

**Blocked by:** none.

**Status:** resolved

- [ ] RedM profile → `redm://connect/...` (query flags unchanged).
- [ ] Legacy/Enhanced behaviour unchanged.
