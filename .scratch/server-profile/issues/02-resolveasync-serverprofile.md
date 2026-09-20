# 02: ResolveAsync devuelve ServerProfile con identidad y requisitos

**What to build:** el tracer bullet de la fase ServerProfile. Dada una dirección (CFX id, o `cfx.re/join/<id>` con o sin esquema), resolver el servidor devuelve ahora un `ServerProfile` que agrupa la identidad (`CfxId`, `ProjectName`) y los `Requirements` derivados de la información que CFX publica. `ResolvedServer` se sustituye por `ServerProfile`. El resolver compone internamente la consulta a CFX y la derivación de requisitos. Se conserva la validación actual: dirección vacía → excepción; servidor no encontrado → excepción.

**Blocked by:** 01 (Ampliar ServerRequirements con Pure Mode y Steam ticket)

**Status:** ready-for-agent

- [ ] Resolver una CFX id válida devuelve un `ServerProfile` con ese `CfxId`
- [ ] El `ServerProfile` incluye el `ProjectName` publicado por el servidor
- [ ] El `ServerProfile` incluye los `Requirements` derivados (GameBuild, PureMode, RequestSteamTicket) tal como los publica CFX
- [ ] `cfx.re/join/<id>` con esquema y sin esquema siguen resolviéndose con el id correcto
- [ ] Dirección vacía y servidor no encontrado siguen lanzando la excepción actual (no regresión)
- [ ] `LauncherSettings` no se modifica; la lógica vive en el dominio