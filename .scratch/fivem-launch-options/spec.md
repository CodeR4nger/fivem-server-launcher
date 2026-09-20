Status: ready-for-agent
Type: spec

# FiveMLaunchOptions — preparar el lanzamiento a partir de los requisitos del servidor

## Problem Statement

`ServerProfile` ya resuelve un servidor y deriva sus `Requirements` (Game Build, Pure Mode, Steam ticket) desde lo que CFX publica. El launcher todavía no puede traducir esa información a lo que FiveM necesita al lanzarse: qué args de línea de comandos pasar (`-b`, `-pure_`, `-cl2`) o qué URI de conexión abrir (`fivem://connect/<addr>`). Hoy no existe representación de esas opciones de lanzamiento, así que la capa que conecta los requisitos con la ejecución de FiveM no puede testearse ni implementarse de forma controlada.

## Solution

Aparece `FiveMLaunchOptions`: un valor inmutable del dominio que describe cómo lanzar FiveM para una intención dada (conectar a un servidor o abrir el cliente directamente). Se construye a partir de un `ServerProfile` (conectando) o de preferencias/Dev Mode (abriendo directamente), y sabe serializarse a una URI `fivem://` o a una lista de argumentos de línea de comandos. No lanza procesos ni toca archivos: solo modela y serializa, para que la futura capa de lanzamiento consuma algo ya probado.

## User Stories

1. Como jugador, quiero que al conectar a un servidor el launcher genere una URI `fivem://connect/<addr>` a partir del `ServerProfile`, para que el cliente se abra apuntando directamente al servidor.
2. Como jugador, quiero que la URI incluya el `GameBuild` del servidor (`-b<build>`) cuando CFX lo publica, para arrancar en el build correcto y evitar el reinicio del cross-build.
3. Como jugador, quiero que la URI incluya el `PureMode` del servidor (`-pure_<nivel>`) cuando CFX lo publica, para arrancar en el pure mode exigido.
4. Como jugador, quiero que la URI **no** incluya args para requisitos ausentes (`null`), para no asumir valores que el servidor no publica.
5. Como jugador, quiero que los args de línea de comandos (`-b`, `-pure_`, `-cl2`) se serialicen para abrir FiveM directamente (Dev Mode / abrir cliente), de forma independiente de la URI de conexión.
6. Como jugador, quiero que la serialización distinga **conectar** (address presente → URI) de **abrir directo** (sin address → args), para conservar la separación conectar-vs-abrir de DOC.md.
7. Como jugador, quiero que `-cl2` (segunda instancia) aparezca solo cuando el usuario lo pide explícitamente (Dev Mode), para no lanzar siempre una instancia adicional.
8. Como jugador con **FiveM Enhanced**, quiero que el launcher **no** genere URI `fivem://connect` ni args `-b`/`-pure_`/`-cl2`, ya que ese cliente no los soporta (verificado), y conectarse ocurre dentro de su propia UI.
9. Como jugador, quiero que cuando no hay requisitos y no hay flags de Dev Mode, la serialización devuelva una URI/args vacíos o sin extras, para reflejar "lanzar normal, sin modificar nada".
10. Como desarrollador, quiero que `FiveMLaunchOptions` dependa solo de tipos de dominio (`ServerProfile`, `ServerRequirements`, `GameClient`) y no de la UI ni de servicios, para poder testearlo sin procesos ni filesystem.
11. Como desarrollador, quiero que el modelo valide su estado (p.ej. address malformada o GameClient inválido) al construirse, para no serializar lanzamientos imposibles en silencio.
12. Como desarrollador, quiero que la fuente de `GameBuild`/`PureMode` al conectar sea siempre `ServerRequirements` (CFX-published), para que los requisitos del servidor ganen sobre cualquier manual.
13. Como desarrollador, quiero que un `FiveMLaunchOptions` construido directamente con pocos campos siga siendo serializable, para servir en pruebas y en casos simples (abrir cliente sin servidor).

## Implementation Decisions

- **Seam único (confirmado con el usuario): módulo `FiveMLaunchOptions`** en `Domain`. Un valor inmutable con fábrica estática y dos serializaciones (`ToUri()` y `ToCommandLineArgs()`), testeado directamente. No se crean seams de Service ni de Configuration para esta fase.
- **Forma del modelo** (decisiones ricas de un prototipo; se trunca a lo esencial):
```csharp
public sealed record FiveMLaunchOptions
{
    public string? Address;        // cfx.re/join/<cfxId> al conectar; null al abrir directo
    public GameClient? GameClient; // FiveM | FiveMEnhanced | RedM; del ServerProfile o PreferredClient
    public int? GameBuild;         // -b<build>
    public int? PureMode;          // -pure_<nivel>
    public bool SecondClient;      // -cl2 (Dev Mode)

    public Uri? ToUri();                        // null si Address o GameClient es FiveMEnhanced
    public IReadOnlyList<string> ToCommandLineArgs();
}
```
- **`ToUri()`**: `fivem://connect/<address>`; añade `?-b<build>` y `?-pure_<nivel>` según los requisitos presentes (formato de parámetros con `?` como separador, verificado en doc/código: `fivem://connect/<servidor>?<params>`). Devuelve `null` cuando no hay `Address` o cuando `GameClient` es `FiveMEnhanced`.
- **`ToCommandLineArgs()`**: lista de args para abrir el cliente directamente: `-b<build>`, `-pure_<nivel>`, `-cl2` (solo si `SecondClient`). **No** incluye dirección de servidor. Para `FiveMEnhanced` devuelve lista vacía (sin `-b`/`-pure_`/`-cl2`, verificado).
- **`GameClient` se incluye** en el modelo (confirmado con el usuario) para poder negar la serialización compatible con Enhanced. Al conectar se toma del `ServerProfile.GameClient`; al abrir directo lo **decide el llamador** (p.ej. la UI, leyendo `LauncherSettings.PreferredClient` a su nivel, y pasándolo al modelo). Esta fase no introduce fábricas ni código que lean `LauncherSettings`.
- **Construcción**: fábrica estática que valida el estado (address malformada, `GameClient` inválido) y un constructor/record que acepta los campos; los requisitos se copian desde `ServerRequirements` con nulos ausentes. Nada de IO ni procesos.
- **Conectar vs abrir**: dos intenciones del mismo modelo, distinguidas por la presencia de `Address`: con `Address` se usa `ToUri()`; sin `Address`, `ToCommandLineArgs()`. Coincide con el uso doble de la forma ya acordado.
- **Vocabulario según DOC.md**: `FiveMLaunchOptions`, `ServerProfile`, `Requirements`, `GameClient`. No se toca `LauncherSettings`; no se añaden campos per-server.
- Restricciones DOC.md respetadas: RSC nunca lo maneja el launcher, Steam/Discord no entran aquí, los requisitos del servidor ganan al conectar, y Dev Mode solo aplica al abrir directamente.

## Testing Decisions

- Un buen test verifica **comportamiento externo observable**: dado un estado conocido, `ToUri()` devuelve la URI esperada (o `null` para Enhanced / sin address) y `ToCommandLineArgs()` devuelve los args esperados (o vacíos). Nunca expone detalles de implementación ni hace IO.
- **Módulo a testear:** `FiveMLaunchOptions` (seam único, testeable directo sin HTTP ni filesystem). No hay seams de Service involucrados en esta fase.
- **Prior art en el repo:** `tests/.../Domain/ServerRequirementsResolverTests.cs` (mapping directo de valores, estilo Given/When/Then, nulos ausentes) y `tests/.../Domain/ServerResolverTests.cs`. Los nuevos tests siguen ese patrón; casos por intención: conectar (URI) y abrir directo (args), más el caso Enhanced (negación).
- Naming: `<Método>_Should<Expectativa>` con comentarios Given/When/Then (convención del repo).

## Out of Scope

- **Lanzar procesos**: `Process.Start`/ejecutar FiveM, `IProcessRunner` o equivalentes. Esta spec modela y serializa únicamente.
- **`fivem://connect` hacia Enhanced**: verificado que no aplica; solo se abre el cliente.
- **Resolución IP:puerto / dominio:puerto** (TODO abierto de `ServerResolver`).
- **CitizenFX.ini**, pool sizes, Steam/Discord, RSC.
- **UI/MVVM**: `MainView.xaml` sigue estático y sin binding.
- **Steam ticket**: `RequestSteamTicket` no participa en la serialización de lanzamiento (es preparación de Steam, otra fase).
- **Validación de `CfxId`** (TODO abierto): este modelo valida su propio estado, no el de la entrada de `ServerResolver`.

## Further Notes

- DOC.md:523: "Si una decisión técnica depende de información actual de FiveM/CFX, investigarla antes de asumirla". Esta spec incorpora la investigación verificada de CLI args, `PureModeState.h`, `CrossBuildSwitch.cpp`, `CitizenFX.ini` y el estado de FiveM Enhanced (sin `-cl2`, sin `+set moo`, pure mode siempre activo, solo último gamebuild, conexión rediseñada sin reinicio).
- `ServerResolver` aún expone `ExtractCfxId` (prefijo `cfx.re/join/`); la address de conexión para la URI se deriva del `CfxId` del `ServerProfile`, no de `EndPoint` (eliminado en la fase anterior).
- Tras implementar: actualizar `AGENTS.md`, marcar casillas como `[x]` y commitear con conventional commits en inglés.