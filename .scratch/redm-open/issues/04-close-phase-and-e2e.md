# 04: Close phase + manual E2E

**What to build:** close the RedM phase: full suite green, docs reconciled (AGENTS.md, DOC.md note
the scheme and probing rules), tickets resolved. Manual E2E on a machine with RedM installed:
dropdown shows RedM, opening launches it, connecting to an rdr3 CFX server uses `redm://connect`.

**Blocked by:** 01, 03.

**Status:** resolved

**Resolution notes:**
- Full suite green (299); docs reconciled (AGENTS.md: scheme per client + RedM probing; DOC.md line
  on serialization schemes).
- Manual E2E (user): server connection works in RedM; dropdown behaviors verified during dev-client
  toggle testing; commits `62e5403` (feature) and view fixes in `81e8854`/`32799b4`.

- [x] Full suite green; docs reconciled.
- [x] Manual E2E: RedM appears and opens from the dropdown when installed.
- [x] Manual E2E: connecting to an rdr3 server uses `redm://connect`.
