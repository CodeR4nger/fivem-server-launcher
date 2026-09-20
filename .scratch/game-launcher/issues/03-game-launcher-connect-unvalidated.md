# 03: GameLauncher conecta un perfil no validado (IP:port/dominio) directo

**What to build:** un perfil no publicado en CFX (`IsCfxValidated=false`) tiene `CfxId` vacío y `Address` crudo (`149.56.120.52:30320`). `FiveMLaunchOptions.FromServerProfile` hoy fuerza `FromCfxId(CfxId)` → falla. La decisión de la spec: `FromServerProfile` elige `CfxId` si existe, si no `Address` (si clasifica como IpPort/DomainPort), y `FiveMLaunchOptions.Create` acepta direcciones IpPort/DomainPort (además de join). Resultado: `fivem://connect/149.56.120.52:30320`.

**Blocked by:** 02

**Status:** resolved

- [x] `FiveMLaunchOptions.Create` acepta `Address` IP:port/dominio válido (vía `ServerAddress.Classify`); cadenas basura siguen lanzando `InvalidAddressException`.
- [x] `FromServerProfile` (`ProfileAddress`): `CfxId` no vacío → join; si no y `Address` IpPort/DomainPort → directo.
- [x] `ConnectAsync` con perfil no validado IpPort → seam recibe `fivem://connect/149.56.120.52:30320`; resultado `Connect`.
- [x] Tests de regresión de `FiveMLaunchOptions` (join, basura) siguen verdes; se añaden `Create_WithIpPortAddress_ShouldAccept`, `Create_WithDomainPortAddress_ShouldAccept`, `Create_WithMalformedIpPort_ShouldThrow`.
- [x] Suite completa verde (106).

## Comments