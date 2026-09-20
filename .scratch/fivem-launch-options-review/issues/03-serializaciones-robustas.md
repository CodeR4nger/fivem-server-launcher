# 03: Robust serializations — truly immutable list and single Enhanced guard

**What to build:** `ToCommandLineArgs` returns a truly read-only list (today it is a mutable `List<string>` typed as `IReadOnlyList`, which a consumer could cast and mutate). The `FiveMEnhanced` negation and the flag building (`-b`/`-pure_`) stop being duplicated between `ToUri` and `ToCommandLineArgs`; the `GameClient` property stops shadowing the enum type.

**Blocked by:** 01 (Harden and seal the factory validation)

**Status:** resolved

- [x] `ToCommandLineArgs` returns a truly immutable collection (not mutable via cast)
- [x] The `FiveMEnhanced` condition and flag building are shared between `ToUri` and `ToCommandLineArgs` (no duplication)
- [x] The `GameClient` property does not shadow the `Core.Enums.GameClient` type
- [x] Serialization behavior identical to before (no regression): same URI, same args, Enhanced → null/empty
- [x] The full suite stays green
