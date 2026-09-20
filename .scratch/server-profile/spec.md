Status: ready-for-agent
Type: spec

# ServerProfile — preparación de requisitos desde información CFX

## Problem Statement

Hoy el launcher solo resuelve una dirección CFX a un `CfxId` (`ServerResolver.ResolveAsync`). DOC.md plantea la fase `ServerProfile → Requirements → FiveMLaunchOptions ↔ CFX server information`: al apuntar a un servidor, el launcher debe conocer qué requisitos publica ese servidor (Game Build, Pure Mode, Steam ticket) para preparar FiveM antes de conectar, **sin duplicar manualmente** en configuración lo que el servidor ya publica y **sin forzar** requisitos al usuario (la filosofía es "no molestar si todo está listo", pero tampoco asumir nada que el servidor no diga).

## Solution

Al resolver una dirección, `ServerResolver.ResolveAsync` devuelve un `ServerProfile` que agrupa la identidad del servidor y sus `Requirements` derivados **exclusivamente** de la información que CFX publica (`sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`). El usuario podrá conectar a un servidor con la seguridad de que el launcher prepara lo que ese servidor pide, sin campos duplicados ni override manual en esta fase.

## User Stories

1. Como jugador, quiero que al ingresar una dirección CFX se resuelva el servidor y su estado, para poder decidir si entrar.
2. Como jugador, quiero que el launcher determine el Game Build que el servidor **impone** (`sv_enforceGameBuild`), para lanzar FiveM con el build correcto sin que yo lo configure manualmente.
3. Como jugador, quiero que el launcher determine el Pure Mode que el servidor pide (`sv_pureLevel`), para que mi sesión cumpla con el modo del servidor.
4. Como jugador, quiero que el launcher detecte si el servidor requiere ticket de Steam (`requestSteamTicket`), para saber si hace falta preparar Steam antes de conectar.
5. Como jugador, quiero que los requisitos vengan de lo que el servidor publica y no de una copia manual mía, para que la info no quede desactualizada ni duplicada.
6. Como jugador, quiero que cuando un servidor **no** publique un requisito (vars ausentes), ese requisito quede ausente/indeterminado, para que el launcher no asuma un valor arbitrario.
7. Como jugador, quiero que el resultado de resolver una dirección incluya el nombre del servidor, para poder mostrarlo en la UI más adelante.
8. Como jugador, quiero conservar el comportamiento existente de validación de direcciones (vacías, no encontradas), para que la resolución siga siendo robusta.
9. Como jugador, quiero que `cfx.re/join/<id>` (con o sin esquema) siga resolviéndose igual, para no romper el flujo actual de entrada.
10. Como desarrollador, quiero que la lógica de requisitos esté en el dominio y no en la UI/XAML, para poder testearla sin procesos ni filesystem.
11. Como desarrollador, quiero que el mapping CFX→requisitos tenga una fuente única de verdad, para no duplicar parsing entre capas.
12. Como jugador, quiero que al abrir FiveM **directamente** (Dev Mode, sin servidor) no se apliquen requisitos de ningún servidor, para conservar la separación conectar-vs-abrir.
13. Como jugador, quiero que los requisitos derivados no se mezclen con la configuración global del launcher (`LauncherSettings`), para mantener `Configuration` solo con preferencias globales.

## Implementation Decisions

- **`ServerResolver` se extiende como seam principal** (confirmado con el usuario): `ResolveAsync` pasa a devolver un `ServerProfile` en lugar de `ResolvedServer`. Internamente compone `CfxService` + `ServerRequirementsResolver` (o la pieza que derive requisitos). Se mantiene la validación actual de dirección (vacía → excepción; no encontrado → excepción).
- **`ResolvedServer` se sustituye/evoluciona a `ServerProfile`** en `Domain`: agrupa identidad (`CfxId`, `ProjectName`) y `Requirements`. Un único modelo de salida del resolver.
- **`ServerRequirementsResolver` se amplía** (building block, seam testable directo): `Resolve(CfxServerInfo)` produce todos los requisitos que CFX publica. Hoy solo existía `GameBuild`; se añaden los demás campos derivados.
- **`ServerRequirements` cubre solo CFX-derived**: `GameBuild` ← `sv_enforceGameBuild`, `PureMode` ← `sv_pureLevel`, `RequestSteamTicket` ← `requestSteamTicket`. Cada campo continúa siendo nullable: ausente = no publicado = indeterminado (evita asumir valores arbitrarios).
- **`CfxServerInfo` ya expone** `EnforceGameBuild`, `PureLevel`, `RequestSteamTicket`; `CfxService` ya los parsea. No cambia `Service`.
- **`LauncherSettings` no se toca**: sigue siendo solo `PreferredClient` + `AutoLaunch`.
- **Sin dev-mode en esta fase**: abrir FiveM directamente no usa requisitos de servidor (constraint de DOC.md); se especifica como comportamiento de usuario pero no se añade UI ni comandos.
- Vocabulario del dominio conforme a DOC.md: `ServerProfile`, `Requirements`, `FiveMLaunchOptions` (este último aún no se construye en esta spec; solo `Requirements`).

## Testing Decisions

- Un buen test aquí verifica **comportamiento externo observable**, no detalles de implementación: dado un `CfxServerInfo` (o una respuesta HTTP fakesca), el perfil resultante expone los requisitos esperados; cuando una var está ausente, el requisito queda `null`.
- **Módulos a testear:** `ServerResolver` (seam alto: identidad + composición + validación de dirección) y `ServerRequirementsResolver` (mapping var→requisito, incl. caso ausente).
- **Prior art en el repo:** `tests/.../Domain/ServerResolverTests.cs` (fakes HTTP vía `FakeHttpMessageHandler`, estilo Given/When/Then, excepciones) y `tests/.../Domain/ServerRequirementsResolverTests.cs` (mapping directo). Los nuevos tests siguen ese patrón, sin librería de mocking.
- La respuesta HTTP se fakea con `FakeHttpMessageHandler` y fixtures JSON como raw strings C# (convención del repo).
- Naming: `<Método>_Should<Expectativa>` con comentarios Given/When/Then.

## Out of Scope

- **Resolución IP:puerto / dominio:puerto** en `ServerResolver` (TODO abierto; requiere investigación antes de asumir).
- **Campos manuales de `ServerProfile.Requirements`** (Steam/Discord como requisitos manuales per-server): otra iteración.
- **`FiveMLaunchOptions`** (argumentos de línea de comandos, CitizenFX.ini, `-cl2`, `fivem://connect`): fase posterior; requiere investigar aplicación final antes de modelarla.
- **UI/MVVM**: `MainView.xaml` sigue estático y sin binding.
- **Dev Mode** y sus overrides de build/pure al abrir FiveM directamente.
- Steam/Discord como detección de procesos o preparación de apps externas.

## Further Notes

- DOC.md:516-517: "Si una decisión técnica depende de información actual de FiveM/CFX, investigarla antes de asumirla". La resolución IP/domain y las opciones de lanzamiento son exactamente ese caso y por eso quedan fuera.
- `GameClient` (FiveM | FiveMEnhanced | RedM) ya viaja en `CfxServerInfo`; no se expande en esta spec pero el `ServerProfile` no debe perderlo (podría usarse en `Requirements` más adelante).
- ADRs del repo en `docs/adr/` importados por `docs/agents/domain.md`; al no existir ADR aún para este área, esta spec es la base para futura decisión ADR.
- Tras implementar, actualizar `AGENTS.md` (estado actual) y commmitear con mensajes conventional commits en inglés.