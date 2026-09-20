# 04: Cerrar huecos de tests

**What to build:** los tests de `FiveMLaunchOptions` cubren las afirmaciones que los tickets 01-03 del review dejan sin verificar: inmutabilidad del valor, inmutabilidad real de la lista de args, que los requisitos provienen de `ServerRequirements` del perfil sin override manual, y se repara el test vacuoso `ToCommandLineArgs_ShouldNotIncludeServerAddress` (hoy pasa trivialmente con args vacíos). Verifica solo comportamiento observable, no detalles de implementación.

**Blocked by:** 03 (Serializaciones robustas — lista inmutable real y guarda Enhanced única)

**Status:** resolved

- [x] Un test verifica que el valor de `FiveMLaunchOptions` no puede mutarse tras construirse
- [x] Un test verifica que la lista de args es de solo lectura (mutar lanza)
- [x] Un test verifica que `FromServerProfile` toma `GameBuild`/`PureMode` de `ServerRequirements` del perfil (manipulando el perfil y comprobando la URI/args resultante)
- [x] El test de "no incluye dirección de servidor" falla por la razón correcta (con args no vacíos o mutando un backing no inmutable)
- [x] La suite completa sigue verde