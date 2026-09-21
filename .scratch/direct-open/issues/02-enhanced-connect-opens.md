# 02: Enhanced connect opens the client

**What to build:** connecting to a `FiveMEnhanced` server actually opens the Enhanced client. The
connect flow's no-URI branch stops returning an inert "open later" result and instead invokes the
open action, so the outcome is `OpenClient` (launched), `NotInstalled`, or `StartFailed` — closing
the documented gap where the connect flow only showed "Opening FiveMEnhanced..." without ever
opening it.

**Blocked by:** 01 (open action at the Application seam).

**Status:** resolved

- [x] Connecting to an installed Enhanced profile opens the Enhanced executable through the process
      seam and returns `OpenClient`.
- [x] Connecting to an Enhanced profile whose executable is missing returns `NotInstalled`.
- [x] Existing connect outcomes (URI connect, start failure) are unchanged.