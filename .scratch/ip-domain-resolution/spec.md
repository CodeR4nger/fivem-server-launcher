Status: resolved
Type: spec

# Resolución de direcciones IP:puerto y dominio:puerto en ServerResolver (con validación)

## Problem Statement

Hoy `ServerResolver.ResolveAsync` solo soporta una dirección CFX (`cfx.re/join/<id>`, con o sin esquema, o un `cfxId` pelado). El TODO en el código (`//TODO: Implement IP and domain filter`) y DOC.md (sección Address, líneas 111-127) exigen soportar también `IP:puerto` y `dominio:puerto` (p.ej. `149.56.120.52:30320`, `play.example.com:30120`). Además, la extracción actual (`ServerAddress.ExtractCfxId`) no valida la forma de la dirección (TODO explícito en AGENTS.md), y `HasServerFormWithNonEmptyId` solo cubre la forma `cfx.re/join/`. DOC.md:530-534 obliga a investigar antes de asumir decisiones técnicas; esta spec incorpora lo investigado sobre cómo CFX identifica servidores por endpoint.

## Solution

`ServerResolver.ResolveAsync` acepta las **cuatro formas** de DOC.md (CFX ID, URL CFX, IP:puerto, dominio:puerto) y devuelve un `ServerProfile` o lanza `InvalidAddressException`. La clasificación y validación de la dirección viven en `ServerAddress` (ya es el dueño de la forma CFX; se extiende como fuente única). Para `IP:puerto`/`dominio:puerto` el launcher intenta resolver el servidor a través de CFX; si el servidor **no está publicado/validado en CFX**, conecta directo igual pero el `ServerProfile` queda marcado como **no validado** para que la UI (en una fase posterior) muestre una advertencia, sin bloquear la conexión.

## User Stories

1. Como jugador, quiero ingresar una dirección `CFX ID` pelada (`8e8xxv`) y que se resuelva como hoy, para no romper el flujo existente.
2. Como jugador, quiero ingresar una URL `cfx.re/join/<id>` (con o sin esquema) y que se resuelva como hoy.
3. Como jugador, quiero ingresar `IP:puerto` (`149.56.120.52:30320`) y que el launcher la resuelva a un perfil de servidor, para conectarme a servidores no accesibles por CFX.
4. Como jugador, quiero ingresar `dominio:puerto` (`play.example.com:30120`) y que se resuelva igual que IP:puerto, para no depender de recordar la IP.
5. Como jugador, quiero que si el `IP:puerto`/`dominio:puerto` **no aparece publicado en CFX**, aún así se pueda conectar (perfil con dirección directa), para entrar a servidores privados o no listados.
6. Como jugador, quiero que un servidor no publicado en CFX quede **marcado como no validado** (pero conectable), para que la UI más adelante me lo advierta sin impedir la conexión.
7. Como jugador, quiero que una dirección inválida (vacía, formato roto, puerto fuera de rango, `IP` mal formada) lance `InvalidAddressException`, para fallar rápido y claro.
8. Como desarrollador, quiero que la clasificación/validación de direcciones sea código testeable puro en `ServerAddress` (sin HTTP, sin filesystem), para cubrir todas las formas con unit tests rápidos.
9. Como desarrollador, quiero no romper los tests existentes de `ServerResolver`/`ServerAddress` (CFX-only), manteniendo compatibilidad con la forma CFX.

## Implementation Decisions

- **`ServerAddress` se extiende como fuente única de la forma de la dirección.** Añade (a) clasificación de la dirección en un enum (`CfxId`, `CfxJoinUrl`, `IpPort`, `DomainPort`, desconocida/inválida) y (b) validación/extracción por forma. `ExtractCfxId` y `HasServerFormWithNonEmptyId` continúan existiendo y se reexpresan sobre el nuevo clasificador (sin romper call-sites existentes).
- **`ServerResolver.ResolveAsync` ramifica por forma:**
  - `CfxId` / `CfxJoinUrl` → comportamiento actual (buscar en CFX por id; `null` → `InvalidAddressException`).
  - `IpPort` / `DomainPort` → intentar resolver vía CFX; si la resolución devuelve un perfil → perfil validado; si no → **perfil conectable no validado** con la dirección tal cual, sin requisitos derivados (no inventar `ProjectName`/`GameClient`/`Requirements`).
  - Forma desconocida/inválida → `InvalidAddressException`.
- **`ServerProfile` gana un marcador de validación** (p.ej. `bool IsCfxValidated`) para distinguir "perfil completo desde CFX" de "perfil conectable directo sin validar". En esta fase se usa como salida del resolver; el consumo en UI (advertencia) es de otra fase.
- **Mecanismo de búsqueda IP/dominio en CFX (investigado y verificado):**
  - No existe un lookup ligero por endpoint. El catálogo completo se sirve en `https://frontend.cfx-services.net/api/servers/streamRedir/` como un **stream binario de frames** (uint32 LE length prefix + mensaje protobuf `master.Server`), que supera 5MB y **ignora query params** (no hay paginación/filtros server-side: `?limit=` sigue devolviendo todo). Los filtros son 100% client-side.
  - **`master.Server`** (`EndPoint` + `ServerData`) expone `vars` (map string→string, incl. `gamename`, `sv_projectName`, `sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`), `connectEndPoints` (IP:port o host con `sv_listingHostOverride`), `server`, `clients`, `svMaxclients`... (schema completo verificado en `cfx-api`/`fivem-server-api`).
  - Buscar un endpoint = **descargar el stream completo, decodificar todos los frames, y matchear** `connectEndPoints` contra `ip:port` (o contra el host resuelto) o `EndPoint` contra un cfx id. Es lo que hacen ambas librerías comunitarias.
  - **Decode protobuf con `Google.Protobuf`** (decisión confirmada con el usuario): schema `.proto` propio del repo basado en el verificado; fixtures de prueba en binario.
  - **Cache con TTL** (p.ej. 5 min) para no re-descargar los MB por cada resolución: la descarga del catálogo es cara y el set de servidores cambia lento.
  - **`dominio:puerto`**: resolver DNS → IP, y matchear contra `connectEndPoints` (IP:port o host publicado).
- El resolver **intenta** esta vía y degrade a "conectable no validado" sin lanzar excepción si el endpoint no aparece en el catálogo (o el catálogo no está disponible).
- **Validación mínima estricta:**
  - `IP:puerto` → 4 octetos decimales (0-255) separados por `.` + `:` + puerto (1-65535).
  - `dominio:puerto` → 1+ labels alfanuméricas con `-` unidas por `.` (sin esquema, sin path, sin usuario@) + `:` + puerto (1-65535).
  - Sin whitespace en ningún formato.
- **Dominio/IP y el detalle de `gamename`/requisitos:** si la forma es IP/dominio y CFX no devuelve perfil, no se derivan requisitos (YAGNI: no adivinar build/pure/steam de una dirección cruda).
- **`ServerAddress` sigue siendo el único lugar que conoce la forma `cfx.re/join/<id>`** (`Domain/ServerAddress` ya es "single owner" según AGENTS.md); la nueva clasificación convive ahí.
- En esta fase **no hay UI**: la advertencia de "servidor no validado" es solo un estado del perfil listo para consumo futuro.

## Testing Decisions

- **Módulos a testear:** `ServerAddress` (clasificación + validación/extracción por las 4 formas y casos inválidos), `ServerResolver` (ramificación por forma: CFX resuelve, IP/dominio con perfil CFX devuelto, IP/dominio sin perfil CFX → no validado conectable, forma inválida → excepción) y el **servicio de catálogo** (fetch stream + decode frames + matcheo por `connectEndPoints`/`EndPoint`, con fixtures binarios protobuf).
- **Prior art en el repo:** `tests/.../Domain/ServerResolverTests.cs` (HTTP fake con `FakeHttpMessageHandler`, Given/When/Then, `CreateResolver` helper) y `tests/.../Domain/ServerAddressTests.cs` (puro, sin HTTP). Nuevos tests siguen ese patrón, sin librerías de mocking.
- La respuesta HTTP se fakea con `FakeHttpMessageHandler` y fixtures JSON como raw strings C# (convención del repo); para el catálogo binario, los fixtures son secuencias de bytes/frames protobuf generados en el test (helper de construcción de frames).
- Naming: `<Método>_Should<Expectativa>` con comentarios Given/When/Then.
- El estado "no validado conectable" se verifica por el campo nuevo de `ServerProfile` (comportamiento observable del resolver), no por detalles internos de cómo se intentó CFX.

## Out of Scope

- **UI/advertencia visual** de "servidor no validado": fase posterior que consume `IsCfxValidated`.
- **Cache del catálogo en disco** y políticas de refresco avanzadas (solo TTL en memoria por ahora; persistencia/refresco en background es iteración futura).
- **Detallado completo del catálogo** (íconos, upvotes, players): se mapea solo lo que `ServerResolver` necesita (`gamename`, requisitos, endpoints).
- **`/api/servers/top/{language}`** y otros endpoints de catálogo: no necesarios para lookup por endpoint.
- **IPv6 (`[::1]:30120`)** y otros formatos exóticos: no pedidos por DOC.md.
- **Steam/Discord** como requisitos manuales del perfil (sigue fuera).
- **Modificar `LauncherSettings`** o cualquier config global.
- **Refactor ruidoso de `ServerAddress`**: el clasificador nuevo debe convivir con las APIs actuales sin cambiar firmas existentes salvo donde el comportamiento lo exija (y se testea).

## Further Notes

- DOC.md:516-517, 530-534: la resolución IP/domain y el mecanismo de búsqueda dependen de info actual de CFX → investigación antes de implementar el mecanismo; **investigación realizada y reflejada en Implementation Decisions** (streamRedir, protobuf con Google.Protobuf, cache TTL).
- La librería comunitaria `fivem-server-api` y `cfx-api` (ambas mantenidas) ya implementan el patrón "stream completo + filtro local"; son la referencia de comportamiento para el servicio de catálogo (schema `master.Server` idéntico en ambas).
- DOC.md:127-128 ("Cuando sea posible, una dirección CFX debe poder resolverse mediante la API de CFX") refuerza el diseño "intentar CFX → degradar a conectable".
- DOC.md:514 ("No asumir que proceso iniciado = listo") y 434-435 ("no asumir requisitos") apoyan no inventar requisitos/GameClient en el perfil no validado.
- `GameClient` nullable en `ServerProfile` ya cubre "no publicado"; el perfil no validado sencillamente lo deja `null`.
- Tras implementar: actualizar `AGENTS.md` (estado actual, quitar TODOs de IP/domain y validación) y commitear con conventional commits en inglés.