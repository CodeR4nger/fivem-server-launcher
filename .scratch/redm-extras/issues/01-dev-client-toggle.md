# 01: Dev-mode client toggle

**What to build:** the dev panel exposes a "LEGACY / REDM" switch (session-only). Selecting RedM
makes the dev LAUNCH / LAUNCH WITH SECOND CLIENT buttons open the RedM exe with the flags instead
of legacy `FiveM.exe`. Legacy remains the default when both are absent.

**Blocked by:** none.

**Status:** resolved

- [ ] A `DevClient` (FiveM/RedM) on the VM, switched by a toggle command.
- [ ] `DevLaunchAsync` resolves `client` from the toggle and still applies flags.
- [ ] UI shows current mode and can switch (no persistence, no settings file change).
