# 02: CitizenFX.ini priming for RedM

**What to build:** `CitizenFxPreparer.PrimeAsync` primes RedM `CitizenFX.ini` exactly like Legacy
(validated profiles only, same build + pool rules), using the RedM install directory. Unvalidated /
Enhanced profiles unaffected.

**Blocked by:** none.

**Status:** resolved

- [x] Validated RedM profiles prime `%RedM.app%\CitizenFX.ini` with legacy-equivalent rules.
- [x] Unvalidated / Enhanced profiles no-op (Enhanced gate now explicit FiveM-or-RedM membership).
