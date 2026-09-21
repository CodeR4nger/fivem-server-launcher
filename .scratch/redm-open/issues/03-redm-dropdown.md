# 03: RedM in the OPEN dropdown + open path

**What to build:** `MainViewModel` includes RedM in `OpenCandidates`; the dropdown shows RedM when
installed (never when absent); the open command launches the RedM exe. Preferred-client seeding
works for RedM as well.

**Blocked by:** 02.

**Status:** resolved

- [ ] Dropdown lists RedM when the locator reports it installed.
- [ ] Opening RedM proceeds through the existing `OpenAsync` path with the RedM exe.
- [ ] Preferred-client seeding honours RedM when installed, falls back otherwise.
