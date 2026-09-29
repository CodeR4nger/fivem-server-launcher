# 02: Harden process-launch inputs

**What to build:** A crafted saved-server entry (imported/shared `saved-servers.json`) can smuggle
shell arguments into `explorer.exe`: the cfx-id validation accepts quotes/ampersands, the connect
URI concatenates the address unescaped (query/fragment injection), and the executable path is
quoted without escaping. Tighten the cfx-id charset, escape the address into the connect URI, and
escape the executable path when launching via the shell route.

**Blocked by:** None (can start immediately).

**Status:** resolved (commit `4c2b2e4`; ticket status stale — verified against the code before closing)

- [x] Cfx-id validation rejects characters outside a strict connectable charset (letters, digits,
      dash, underscore)
- [x] The connect URI escapes the address so `?`, `#`, quotes and friends cannot alter it
- [x] The executable path cannot break out of its quoting in the shell launch
- [x] All existing address forms still classify, resolve, and launch exactly as before

## Comments

- Verified in code (2026-09-28): `ServerAddress.IsValidCfxId` enforces the letters/digits/`-`/`_`
  charset and gates `Classify`, `IsCfxJoinUrlWithValidId`, `HasServerFormWithNonEmptyId` and
  `SavedServer.Create` (cfx id rejection). The URI-injection guarantee is achieved by *rejection
  rather than escaping*: `FiveMLaunchOptions.ValidateAddress` only admits `CfxJoinUrl` (strict
  charset) and direct forms (exact-numeric IP octets, letters/digits/`-` domain labels, numeric
  port, no whitespace), so `?`/`#`/quotes can never reach `ToUri()`'s concatenation.
  `GameProcessLauncher.StartExecutableAsync` escapes embedded quotes in the executable path
  before wrapping it for `explorer.exe`. Commit `4c2b2e4` pinned all of it with tests
  (`ServerAddressTests`, `SavedServerTests`, `GameProcessLauncherTests`); suite green.
