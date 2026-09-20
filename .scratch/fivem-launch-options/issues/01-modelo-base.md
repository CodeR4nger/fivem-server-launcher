# 01: Modelo base FiveMLaunchOptions con fábrica validada

**What to build:** el valor inmutable del dominio que describe cómo lanzar FiveM para una intención dada. Expone `Address` (nullable: la address de conexión cuando se conecta a un servidor; `null` al abrir el cliente directamente), `GameClient`, `GameBuild`, `PureMode` y `SecondClient`. Una fábrica estática construye el modelo validando su estado (address malformada → error; `GameClient` inválido/desconocido → error). No lanza procesos ni toca archivos; depende solo de tipos de dominio.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Dado un `FiveMLaunchOptions` construido con campos conocidos, expone `Address`, `GameClient`, `GameBuild`, `PureMode` y `SecondClient` con esos valores
- [x] Se puede construir sin `Address` (intención "abrir directo") sin error
- [x] La fábrica rechaza una address malformada (vacía o sin forma de address de servidor)
- [x] La fábrica rechaza un `GameClient` inválido (valor desconocido del enum)
- [x] El valor es inmutable: asignaciones que lo cambiarían no compilan/fallan en tiempo de diseño
- [x] No referencia UI, servicios ni configuración; solo tipos de dominio