# 05: Phase close (AGENTS.md, release, commit)

**What to build:** the `ip-domain-resolution` phase is documented and released. Update `AGENTS.md` (current state: ResolveAsync supports the 4 forms, `ServerProfile.IsCfxValidated`, streamRedir catalog service + `Google.Protobuf`, remove IP/domain and validation TODOs, new test/dependency counts), verify the build and whole suite green, and commit with English conventional commits.

**Blocked by:** 04

**Status:** resolved

- [x] `AGENTS.md` reflects the real state: 4 address forms in `ServerResolver`, `ServerProfile.IsCfxValidated`, catalog via `streamRedir` with protobuf + TTL cache, DNS seam, `Google.Protobuf`/`Grpc.Tools` dependency.
- [x] Open IP/domain and validation TODOs removed from code/specs.
- [x] `dotnet build` and the whole test suite green (with the final count updated wherever mentioned).
- [x] Commit(s) with English conventional commits (e.g. `feat(domain): resolve ip and domain addresses`, `chore(tickets): ...`).
- [x] Phase paper trail closed: checkboxes of the 5 tickets verified against the code.

## Comments

- All checkboxes of the 5 tickets verified against the code. Suite: **97 green tests** (0 build errors, 0 warnings reported in the build).
- Phase commits (English conventional):
  - `feat(domain): resolve ip and domain addresses with streamRedir catalog`
  - `chore(tickets): close ip-domain-resolution phase`
