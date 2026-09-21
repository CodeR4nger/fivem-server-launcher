# 01: Saved server domain + file repository

**What to build:** the user can store named servers so they persist across launcher restarts. A `SavedServer` carries a name, an address (validated with the existing address classification so only connectable forms are accepted) and manual `RequiresSteam`/`RequiresDiscord` flags. A repository seam exposes get-all/add/update/remove; the real implementation persists a JSON list to a file (mirroring the launcher-settings persistence style), and tests use an in-memory fake plus real-file tests.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `SavedServer` factory rejects blank names and addresses that don't classify as connectable (reuses `ServerAddress.Classify`).
- [x] `IServerRepository` offers get-all, add, update, remove.
- [x] Real file repository persists across restarts (create-when-absent, corrupt file → empty list, not a crash).
- [x] In-memory fake for higher-layer tests.
- [x] Suite green.

## Comments
- Review follow-up: `MatchesAddress` case-insensitive key extracted onto `SavedServer` (DRY across both repos); `Update` on the fake now throws like the real one; `GetAll` drops null/blank/unconnectable persisted entries; `Remove` of an unknown address no longer rewrites the file.