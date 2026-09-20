# 01: Clasificar y validar las 4 formas de dirección en ServerAddress

**What to build:** `ServerAddress` (fuente única de la forma de la dirección) aprende a reconocer y validar las cuatro formas que DOC.md exige: `CFX ID` pelado, URL `cfx.re/join/<id>` (con o sin esquema), `IP:puerto` y `dominio:puerto`. Cada forma valida su sintaxis (octetos 0-255, puerto 1-65535, labels de dominio alfanuméricas con `-` sin esquema/path/usuario@, sin whitespace) y expone la extracción del dato relevante para cada forma (`cfxId`, `ip:port`, `host:port`). Las APIs existentes `ExtractCfxId` y `HasServerFormWithNonEmptyId` se reexpresan sobre el nuevo clasificador sin romper sus call-sites.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Un clasificador (enum) devuelve la forma de una dirección: `CfxId`, `CfxJoinUrl`, `IpPort`, `DomainPort` o desconocida/inválida.
- [x] `cfx.re/join/<id>` y `https://cfx.re/join/<id>` se clasifican como `CfxJoinUrl` y extraen el id (comportamiento actual preservado).
- [x] Un id pelado sin `.`, sin `:` y sin `cfx.re/join/` se clasifica como `CfxId`.
- [x] `IP:puerto` válida (4 octetos 0-255 + puerto 1-65535) se clasifica como `IpPort`; octetos/fuera de rango o puerto 0/65536 → inválida.
- [x] `dominio:puerto` válida (labels alfanuméricas con `-` unidas por `.`, puerto 1-65535, sin esquema/path/usuario@) se clasifica como `DomainPort`; violaciones → inválida.
- [x] Cualquier whitespace en cualquier forma → inválida.
- [x] `ExtractCfxId` y `HasServerFormWithNonEmptyId` siguen compilando y pasando sus tests existentes tras reexpresarse sobre el clasificador.
- [x] Tests puros en `ServerAddressTests` (sin HTTP, sin filesystem), estilo Given/When/Then del repo.

## Comments

- TDD RED→GREEN→REFACTOR completado con 13 tests nuevos en `tests/.../Domain/ServerAddressTests.cs`. Suite completa 75 verdes (62 previos + 13 nuevos).
- Decisión en el camino (regla TLD): el TLD de un `dominio` debe contener al menos una letra para no confundir `999.56.120.52:30320` (IP con octeto inválido) con un dominio numérico.
- REFACTOR aplicado: `HasServerFormWithNonEmptyId` y `Classify` comparten `IsValidCfxId` (DRY), con suite verde.