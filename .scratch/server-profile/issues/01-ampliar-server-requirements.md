# 01: Ampliar ServerRequirements con Pure Mode y Steam ticket

**What to build:** el building block de dominio reconoce los tres requisitos que CFX publica. Hoy `ServerRequirements` solo expone `GameBuild`; se amplía para exponer también el Pure Mode (cuando el servidor publica `sv_pureLevel`) y el requisito de ticket de Steam (cuando publica `requestSteamTicket`). Cada requisito permanece ausente (`null`) cuando el servidor no lo publica; el launcher no asume valores arbitrarios.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Dado un `CfxServerInfo` con `PureLevel`, el resultado expone un `PureMode` con ese valor
- [ ] Dado un `CfxServerInfo` con `RequestSteamTicket`, el resultado expone un `RequestSteamTicket` con ese valor
- [ ] Dado un `CfxServerInfo` sin esas vars, los campos correspondientes quedan `null`
- [ ] El `GameBuild` existente sigue derivándose de `sv_enforceGameBuild` (no regresión)
- [ ] Mapping propulsado por `CfxServerInfo` (fuente única), sin tocar `Service` ni `LauncherSettings`