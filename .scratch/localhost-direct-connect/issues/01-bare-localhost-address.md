# 01: Bare localhost is a valid port-less direct address

**What to build:** A user can type `localhost` (no port, any casing) into the add-server dialog or
the connect box, save it, and connect: it classifies as a port-less domain address, resolves like
any other direct address (default port), and launches a direct connect. Other dotless single
tokens keep classifying as cfx ids; `localhost:<port>` keeps its current domain:port handling.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Bare `localhost` classifies as a port-less domain address (case-insensitive)
- [x] Saving bare `localhost` succeeds and stores a direct-form address entry
- [x] Connecting to bare `localhost` produces a connectable unvalidated profile (listed local
      servers may validate; unlisted degrade to direct) and a direct connect launch
- [x] Any other dotless/colonless token still classifies as a cfx id
- [x] `localhost:<port>` and loopback IP forms behave exactly as before (regression)
- [x] Whitespace-containing or empty variants still classify as invalid
