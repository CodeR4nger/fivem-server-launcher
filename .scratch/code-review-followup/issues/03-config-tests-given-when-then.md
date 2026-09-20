# 03: Configuration tests use Given/When/Then

**What to build:** the tests in the `Configuration` folder follow the AGENTS.md comment convention (`Given/When/Then` with `<Method>_Should<Expectation>` naming), same as the rest of the suite. No behavior or assertion changes, only comment structure.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `ConfigurationRepositoryTests` uses Given/When/Then instead of Arrange/Act/Assert
- [x] `FileSettingsStorageTests` uses Given/When/Then
- [x] `InMemorySettingsStorageTests` uses Given/When/Then
- [x] `LauncherSettingsTests` adds the Given/When/Then comments (currently without them)
- [x] Full suite stays green (purely cosmetic comment change)
