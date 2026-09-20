Status: resolved
Type: spec

# GameLauncher: lanzar la conexión a un servidor desde un ServerProfile

## Problem Statement

El dominio ya es capaz de resumir qué hacer para un servidor (`ServerProfile` → `FiveMLaunchOptions` → URI `fivem://` o args CLI), pero nada orquesta el lanzamiento real. La UI (futura) necesita una capa `Application/Manager` que, dado un `ServerProfile` ya resuelto, decida *qué* lanzar y delegue la ejecución al proceso real.

Los dos resultados posibles de la decisión (verificado contra docs oficiales y código citizenfx/fivem):

- **Cliente conectable (Legacy/RedM con URI):** `fivem://connect/<addr>?-b<build>?-pure_<nivel>` es el mecanismo de conexión directa (protocolo registrado por el propio cliente; el foro oficial confirma `fivem://connect/<server>`, y `connect 127.0.0.1:30120` y `connect cfx.re/join/y4lg95` funcionan).
- **`FiveMEnhanced`:** no soporta `fivem://connect`, `-b`, `-pure_` ni `-cl2` (DOC.md). El launcher solo puede *abrir el cliente*; conectar ocurre dentro de su propia UI.

Además hay un caso que hoy `FiveMLaunchOptions.FromServerProfile` no cubre: un perfil **no validado** (`IsCfxValidated=false`, p.ej. IP:port no publicada en CFX) tiene `CfxId` vacío y `Address` crudo (`149.56.120.52:30320`). `FromServerProfile` fuerza `FromCfxId(CfxId)` → `cfx.re/join/` vacío → `InvalidAddressException`. El GameLauncher debe poder conectarse a ese server directo por su `Address`.

## Solution

Nueva capa `Application` (una sola clase orquestadora + un seam de procesos):

- `GameLauncher.ConnectAsync(ServerProfile)` → devuelve un resultado de lanzamiento (`LaunchResult`):
  - `Connect(Uri)` cuando el perfil serializa un URI `fivem://connect/...` (Legacy/RedM) — tras delegar la ejecución al seam.
  - `OpenClient(GameClient)` para `FiveMEnhanced` (o cliente sin direccionar) — **no** llama al seam de procesos: es la UI quien después abrirá el cliente.
  - Construye las opciones desde el perfil usando `CfxId` si existe (join form) o `Address` directo en caso contrario (IP:port/dominio).
- **Seam nuevo:** `IGameProcessLauncher` con un único método (p.ej. `Task StartAsync(Uri uri)`). Producción: `Process.Start`/ShellExecute del URI `fivem://` (el protocolo lo maneja FiveM). Tests: fake que registra los `Request` sin procesos reales.
- `FiveMLaunchOptions` se extiende (decisión registrada abajo) para permitir que `Address` sea un server address válido que no sea join (IP:port/dominio), conservando las invariantes existentes (creación solo vía `Create`/factories, inmutable, null-guard, reutilizando `ServerAddress`).

### Decisión: `FiveMLaunchOptions.Create` acepta IP:port/dominio

Contradice parcialmente la invariante de la fase `launch-options-invariant-review` (solo admitía `cfx.re/join/`). Razón: un server no publicado en CFX se conecta directo por IP:port y el URI `fivem://connect/149.56.120.52:30320` es el mecanismo oficial. La validación pasa de `HasServerFormWithNonEmptyId` a "null **o** cualquier `ServerAddress.Classify` en `CfxJoinUrl|IpPort|DomainPort`" (la clase de clasificación ya centraliza esto). Se documenta en AGENTS.md.

## User Stories

1. Como jugador, conectarme a un servidor Legacy/RedM publicada en CFX debe lanzar `fivem://connect/cfx.re/join/<id>?-b<build>?-pure_<nivel>` vía el seam de procesos.
2. Como jugador, conectarme a un servidor `FiveMEnhanced` no lanza URI; el launcher devuelve `OpenClient(FiveMEnhanced)` y la UI abrirá el cliente después.
3. Como jugador, conectarme a un servidor no publicado (IP:port/dominio) debe lanzar `fivem://connect/<ip:port>` directo.
4. Como desarrollador, quiero que el seam de procesos sea un único punto (fakeable) para testear el orquestador sin procesos reales.

## Implementation Decisions

- `LaunchResult`: tipo inmutable (record o sealed) con `Kind` (`Connect | OpenClient`) y payload (`Uri?` o `GameClient?`). Factories explícitas; sin ctor público.
- `GameLauncher` recibe `IGameProcessLauncher` por ctor. `ConnectAsync`:
  - Construye `FiveMLaunchOptions` desde el perfil: `CfxId` no vacío → `FromCfxId`; si vacío y `Address` clasifica como IpPort/DomainPort → `Address` directo. `GameClient`/`Requirements` del perfil.
  - `ToUri()` null → `LaunchResult.OpenClient(GameClient ?? PreferredClient fallback del perfil)`. No lanza, no llama al seam.
  - URI presente → llama al seam y devuelve `LaunchResult.Connect(uri)`.
- `FiveMLaunchOptions.Create`: `ValidateAddress` acepta `null` o clasificación `CfxJoinUrl | IpPort | DomainPort` (vía `ServerAddress.Classify`). `FromServerProfile` elige `CfxId`/`Address` según el perfil. Tests existentes de `IpPort` inválido siguen: un `Address` que no clasifica sigue lanzando.
- Este caso reutiliza `ServerAddress` (dueño de la clasificación), no duplica regex.
- `IGameProcessLauncher` en `Application`. Fake en tests (`FakeGameProcessLauncher`) que registra URIs recibidos y expone `RequestCount`.
- Fuera de alcance: PathResolver/detección de instalación, Dev Mode, `-cl2`, Steam/Discord, checks de estado, splash/progress, implementación real del seam con `Process.Start` (se deja el contrato; la UI la conectará).

## Testing Decisions

- TDD en el seam de `GameLauncher` (solo el seam confirmado con el usuario: `GameLauncher.ConnectAsync` → `LaunchResult`, con `IGameProcessLauncher` fake).
- Tests por slice vertical:
  1. Perfil Legacy con CfxId + requirements → seam recibe `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1`; resultado `Connect`.
  2. Perfil `FiveMEnhanced` → resultado `OpenClient`, seam NO llamado.
  3. Perfil no validado con `Address` IP:port → seam recibe `fivem://connect/149.56.120.52:30320`; resultado `Connect`.
  4. Regresión `FiveMLaunchOptions`: `Create` con IP:port/dominio válido no lanza; con cadena basura sigue lanzando `InvalidAddressException`.
- Naming `<Method>_Should<Expectation>` con Given/When/Then.

## Out of Scope

- Implementación real de `IGameProcessLauncher` con `Process`/ShellExecute.
- PathResolver / detección de instalación de FiveM/Enhanced.
- Dev Mode, `-cl2`, Steam/Discord requirements, status checks.
- MVVM / wiring de UI.

## Further Notes

- Mecanismo verificado: protocolo `fivem://connect/<server>` (usado por el propio client), foro Cfx.re y docs fivem.net confirman conexión directa por IP:port y cfx.re/join. `connect 127.0.0.1:30120` funciona vía URI.
- `DOC.md` sitúa `GameLauncher` en la capa Application; mantener la lógica fuera de XAML y de procesos reales.
- Al cerrar: actualizar AGENTS.md (capa Application, seam `IGameProcessLauncher`, `FiveMLaunchOptions.Create` aceptando IpPort/DomainPort), commit convencional en inglés.