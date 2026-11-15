# 02: Saved-server data carries build, pure mode and Steam enforcement

**What to build:** the data the Servers page's detail pane needs, with no UI change yet. The
saved-server presence built from the catalog snapshot also carries the game build the server
enforces, its pure mode and whether it enforces Steam, mapped through the shared catalog
variable mapping (no new requests). Each saved-server row exposes what the detail pane will
show: players text, build label (number plus DLC name from the game-build name data, bare
number when unnamed, "Server default" when not published), pure mode label ("Off" when not
published), and the effective requirements (manual Steam/Discord flags merged with the
published Steam flag by the same rule connecting uses: manual Steam is additive). Offline and
not-listed rows keep their last known values hidden behind a clear offline/not-listed state;
loopback rows with manual connection parameters show those.

See `../spec.md` (Implementation Decisions → Server detail data).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Presence carries build, pure mode and Steam enforcement for listed servers; absent vars → none, never a guess.
- [ ] The row's build/pure/requirements labels follow the rules above, re-emitted on language change and on the game-build data swap.
- [ ] Requirements merge manual and published flags exactly as connecting does (tests mirror the existing `ForConnection` cases).
- [ ] Loopback rows with manual parameters show the manual build/pure/game.
- [ ] Tests at the enrichment-service and row view-model seams; new localization keys in all 11 languages; suite green, zero warnings.
