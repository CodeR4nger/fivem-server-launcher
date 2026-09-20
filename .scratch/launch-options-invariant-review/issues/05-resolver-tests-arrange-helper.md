# 05: Arrange helper in ServerResolverTests (DRY)

**What to build:** In `tests/FiveMServerLauncher.Tests/Domain/ServerResolverTests.cs`, the repeated `new ServerResolver(cfxService, new ServerRequirementsResolver())` (10×) is centralized in a private arrange helper (method or local factory) that takes the `cfxService` and returns the resolver. No change to the tested logic or the assertions.

**Blocked by:** None

**Status:** resolved

- [x] The creation of `new ServerResolver(...)` is no longer repeated inline in the test methods; it uses the helper
- [x] The 9+ existing tests reason the same (same Given/When/Then and assertions)
- [x] Full suite green (62)

## Comments
- Private helper `CreateResolver(CfxService)` centralizes `new ServerResolver(cfxService, new ServerRequirementsResolver())`; the 10 inline occurrences were replaced.
