# 02: Effective Steam/Discord requirements

**What to build:** the server requirements recognize Steam and Discord. Steam has an automatic source — the server-published `sv_enforceSteamAuth` (true = hard requirement, per source investigation; `requestSteamTicket` stays a soft request and is NOT a steam requirement) — plus the manual flag from the saved server. Discord is manual-only. Resolving produces effective `SteamRequired`/`DiscordRequired`: published wins over manual when both exist.

**Blocked by:** 01 (Saved server domain + file repository) — the manual flags live on `SavedServer`

**Status:** resolved

- [x] `ServerRequirements` exposes effective `SteamRequired` and `DiscordRequired` (nullable; absent = indeterminate).
- [x] CFX mapping reads `sv_enforceSteamAuth` (strict `"true"`/`"false"`) in both the `/single/` and catalog paths.
- [x] Resolver merge: published `sv_enforceSteamAuth` wins over the manual flag; Discord uses the manual flag only; `requestSteamTicket` keeps its existing meaning unchanged.
- [x] Merge logic is pure domain and unit-tested (published true/false vs manual true/absent both directions).
- [x] Suite green (193).

## Comments
- Review follow-up: the merge (`ServerRequirements.ForConnection`, record `with`) is applied inside `ServerResolver.ResolveAsync(string, SavedServer?)` so `ServerProfile.Requirements` is always the effective connection requirements; the no-saved-server overload keeps the existing single-arg entry point.