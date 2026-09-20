# 03: Serializaciones robustas — lista inmutable real y guarda Enhanced única

**What to build:** `ToCommandLineArgs` devuelve una lista de solo lectura de verdad (hoy es una `List<string>` mutable tipada como `IReadOnlyList`, que un consumidor podría castear y mutar). La negación por `FiveMEnhanced` y el armado de flags (`-b`/`-pure_`) dejan de estar duplicados entre `ToUri` y `ToCommandLineArgs`; la propiedad `GameClient` deja de ensombrecer el tipo enum.

**Blocked by:** 01 (Endurecer y blindar la validación de la fábrica)

**Status:** resolved

- [x] `ToCommandLineArgs` devuelve una colección inmutable de verdad (no mutables por cast)
- [x] La condición `FiveMEnhanced` y el armado de flags están compartidos entre `ToUri` y `ToCommandLineArgs` (sin duplicación)
- [x] La propiedad `GameClient` no ensombrece el tipo `Core.Enums.GameClient`
- [x] Comportamiento de las serializaciones idéntico al previo (sin regresión): URI igual, args iguales, Enhanced → null/vacío
- [x] La suite completa sigue verde