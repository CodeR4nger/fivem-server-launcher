# 02: ToCommandLineArgs devuelve ImmutableArray (inmutable de verdad)

**What to build:** `src/FiveMServerLauncher/Domain/FiveMLaunchOptions.cs` cambia `ToCommandLineArgs()` para devolver `ImmutableArray<string>` (add `using System.Collections.Immutable;`) en lugar de `string[]`, haciendo imposible la mutación por cast por índice. La firma cambia de `IReadOnlyList<string>` a `ImmutableArray<string>`. Comportamiento observable idéntico: mismos args, orden `-b` antes de `-pure_`, `-cl2` condicional, vacía para `FiveMEnhanced`.

**Blocked by:** 01

**Status:** resolved

- [x] Firma de `ToCommandLineArgs()` es `ImmutableArray<string>`
- [x] El valor devuelto NO es un `string[]` (test por tipo/reflexión), por lo que `((IList<string>)args)[0] = "x"` no puede mutar el resultado devuelto al llamador
- [x] El test RED previo confirma el fallo contra el `string[]` actual (mutabilidad por índice) antes del cambio
- [x] `ToCommandLineArgs_ShouldReturnReadOnlyList` se adapta al nuevo tipo (el valor es inmutable, contiene los args correctos, y para Enhanced está vacío)
- [x] Args correctos: `["-b3258", "-pure_1", "-cl2"]` con `SecondClient`, y vacía para `FiveMEnhanced`
- [x] Suite completa verde (62)
