# 02: Extract shared host:port split (DRY)

**What to build:** the "last `:` separates host from port" split appears in 3 places with slightly different shapes. Extract a common access so that `ServerAddress.IsIpPort`/`IsDomainPort` and `ServerResolver.GetHost`/`GetPort` share it, removing the duplication without changing behavior.

**Blocked by:** 01 (both touch `ServerAddress`, better in order)

**Status:** resolved

- [x] A single place (helper/method in `ServerAddress` or a host/port pair type) performs the `host:port` split, used by the classification (`IsIpPort`/`IsDomainPort`) and by `ServerResolver` (replaces `GetHost`/`GetPort`).
- [x] No observable behavior change: all current classifications and resolutions stay the same.
- [x] Full suite green (the previous phase's tests are the refactor safety net).

## Comments
