# 04: Close the test gaps

**What to build:** the `FiveMLaunchOptions` tests cover the claims that review tickets 01-03 leave unverified: value immutability, true immutability of the args list, that requirements come from the profile's `ServerRequirements` with no manual override, and fix the vacuous `ToCommandLineArgs_ShouldNotIncludeServerAddress` test (today it passes trivially with empty args). Verifies only observable behavior, not implementation details.

**Blocked by:** 03 (Robust serializations — truly immutable list and single Enhanced guard)

**Status:** resolved

- [x] A test verifies that the `FiveMLaunchOptions` value cannot be mutated after construction
- [x] A test verifies that the args list is read-only (mutating throws)
- [x] A test verifies that `FromServerProfile` takes `GameBuild`/`PureMode` from the profile's `ServerRequirements` (tampering with the profile and checking the resulting URI/args)
- [x] The "does not include server address" test fails for the right reason (with non-empty args or mutating a non-immutable backing)
- [x] The full suite stays green
