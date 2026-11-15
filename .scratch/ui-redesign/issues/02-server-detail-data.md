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

**Status:** done

- [x] Presence carries build, pure mode and Steam enforcement for listed servers; absent vars → none, never a guess.
- [x] The row's build/pure/requirements labels follow the rules above, re-emitted on language change and on the game-build data swap.
- [x] Requirements merge manual and published flags exactly as connecting does (tests mirror the existing `ForConnection` cases).
- [x] Loopback rows with manual parameters show the manual build/pure/game.
- [x] Tests at the enrichment-service and row view-model seams; new localization keys in all 11 languages; suite green, zero warnings.

## Comments

- Delivered on `claude/ui-02-detail-data-1799cd`. `ServerPresence` gains `GameBuild`/`PureMode`/`SteamRequired`, read through `CfxVars.ToServerInfo` (the connect route's mapping) from the same snapshot.
- Row members on `SavedServerItem`: `DetailState` (`Listed` / `Manual` / `Offline` / `NotListed`), `ShowsDetails`, `DetailStateLabel`, `PlayersText`, `BuildLabel` ("3258 · Bottom Dollar Bounties", bare number when unnamed, "Server default"), `PureModeLabel` (level or "Off"), `DetailGame`/`DetailGameLabel`, `EffectiveRequiresSteam`/`EffectiveRequiresDiscord`.
- The Steam merge rule moved to `ServerRequirements.SteamRequiredFor(published, manual)`; `ForConnection` and the row both call it (row tests mirror the `ForConnection` cases).
- **Offline decision:** `ServerPresence.Offline` carries no build/pure/Steam; `ApplyPresence` keeps the last known build and pure mode (like the game), but `DetailState = Offline` hides them, and the effective Steam requirement ignores a published flag while offline (only live published data counts, as at connect).
- Loopback: `Manual` when the address is loopback and a manual build/pure/game is set and the server is not live in the catalog (a listed manual cfx id → `Listed`, published wins — the connect rule).
- New keys (all 11 languages): `DetailBuildServerDefault`, `DetailPureModeOff`, `DetailStateOffline`, `DetailStateNotListed`, `DetailStateManual`.
