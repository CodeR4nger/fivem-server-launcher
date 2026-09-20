# 08: Decide on Middle Men: ConfigurationRepository and ServerRequirementsResolver

**What to build:** design decision on two classes that mostly delegate: `ConfigurationRepository` (Save/Load with null-guards) and `ServerRequirementsResolver` (3-line mapper). Two paths: justify them as intentional seams (documenting it in DOC.md and an ADR) or inline them where they are consumed. This is a documentation/refactor ticket depending on what is decided.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] The decision is recorded (ADR in `docs/adr/` or a note in DOC.md)
- [x] If kept: DOC.md explains why they are seams (testability/separation) and their boundary
- [ ] If inlined: the consumer calls the direct target and the intermediate classes are deleted (it was decided NOT to inline)
- [x] Suite green after the change
