# 02: Find saved server by cfx id

**What to build:** The saved-server repository can be queried by cfx id: a saved server carrying
a captured id is retrievable by that id (case-insensitive), enabling the connect flow to find a
saved row's context when the user typed an id-form address instead of the stored direct address.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The repository interface exposes a find-by-cfx-id lookup returning the saved server or none
- [x] The file-backed repository implements it (case-insensitive id match; corrupt/missing store
      still degrades safely)
- [x] The in-memory repository used by tests implements it consistently
- [x] A miss returns none without throwing; an id shared by multiple rows returns the first match
      (asserted as such)
