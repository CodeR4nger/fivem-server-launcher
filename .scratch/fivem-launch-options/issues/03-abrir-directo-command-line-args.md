# 03: Abrir directo — ToCommandLineArgs()

**What to build:** la intención "abrir el cliente directamente" (Dev Mode / abrir FiveM sin servidor, desde preferencias o overrides manuales). Dado un `FiveMLaunchOptions` sin address, `ToCommandLineArgs()` produce la lista de argumentos de línea de comandos para abrir el cliente de FiveM/RedM: `-b<build>`, `-pure_<nivel>` y `-cl2` (solo si `SecondClient`). Para `FiveMEnhanced` devuelve lista vacía (ese cliente no soporta `-b`/`-pure_`/`-cl2`, verificado). Nunca incluye dirección de servidor.

**Blocked by:** 01 (Modelo base FiveMLaunchOptions con fábrica validada)

**Status:** resolved

- [x] Con `GameBuild`, los args incluyen `-b<build>`
- [x] Con `PureMode`, los args incluyen `-pure_<nivel>`
- [x] `-cl2` aparece solo cuando `SecondClient` es `true`
- [x] Sin requisitos ni flags, la lista está vacía
- [x] Con `GameClient = FiveMEnhanced`, la lista está vacía (sin `-b`/`-pure_`/`-cl2`)
- [x] La lista nunca incluye una dirección de servidor
- [x] La lista es de solo lectura