# 03: GameLauncher connects an unvalidated profile (IP:port/domain) directly

**What to build:** a profile not published on CFX (`IsCfxValidated=false`) has an empty `CfxId` and a raw `Address` (`149.56.120.52:30320`). `FiveMLaunchOptions.FromServerProfile` today forces `FromCfxId(CfxId)` → fails. The spec decision: `FromServerProfile` picks `CfxId` if it exists, otherwise `Address` (if it classifies as IpPort/DomainPort), and `FiveMLaunchOptions.Create` accepts IpPort/DomainPort addresses (in addition to join). Result: `fivem://connect/149.56.120.52:30320`.

**Blocked by:** 02

**Status:** resolved

- [x] `FiveMLaunchOptions.Create` accepts a valid IP:port/domain `Address` (via `ServerAddress.Classify`); garbage strings still throw `InvalidAddressException`.
- [x] `FromServerProfile` (`ProfileAddress`): non-empty `CfxId` → join; otherwise `Address` IpPort/DomainPort → direct.
- [x] `ConnectAsync` with an unvalidated IpPort profile → seam receives `fivem://connect/149.56.120.52:30320`; result `Connect`.
- [x] `FiveMLaunchOptions` regression tests (join, garbage) stay green; `Create_WithIpPortAddress_ShouldAccept`, `Create_WithDomainPortAddress_ShouldAccept`, `Create_WithMalformedIpPort_ShouldThrow` are added.
- [x] Full suite green (106).

## Comments
