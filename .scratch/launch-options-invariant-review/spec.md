Status: ready-for-agent
Type: spec

# Cierre de hallazgos del code review — invariantes de FiveMLaunchOptions y DRY en UI/tests

## Problem Statement

El code review de `6f67ba8...HEAD` (31 commits) dejó dos hallazgos del eje Spec que incumplen criterios de aceptación ya marcados como resueltos, y tres del eje Standards (judgement calls). Resumen:

1. **Invariante bypasseable con `with` (Spec, alto)**: `FiveMLaunchOptions` es `sealed record` con props `init` y ctor privado. `options with { Address = "garbage" }` o `{ GameClient = (GameClient)99 }` construye estado inválido sin pasar por `Create`; `ToUri()` lo serializaría como `fivem://connect/garbage` en silencio. Así se violan `01-endurecer-validacion-factory.md:13` ("No es posible construir `FiveMLaunchOptions` con estado inválido fuera de la fábrica (bloqueado en diseño/compilación)") y "Ningún estado inválido se serializa en silencio".
2. **`ToCommandLineArgs` no es inmutable de verdad (Spec, alto)**: devuelve `string[]` (`args.ToArray()`); `((IList<string>)args)[0] = "x"` muta por índice. El test actual solo cubre que `Add` lanze. Viola `03-serializaciones-robustas.md:9` ("colección inmutable de verdad (no mutables por cast)").
3. **Duplicación en `MainView.xaml` (Standards, DRY)**: `DropdownButtonStyle` y `ArrowButtonStyle` re-declaran ~9 setters compartidos que `SecondaryButtonStyle` demuestra que pueden heredar con `BasedOn`.
4. **Duplicación en `MainWindow.xaml` (Standards, DRY)**: los botones Minimizar/Cerrar repiten el mismo conjunto de atributos; `Height="40"` recursa en caption/row/botones.
5. **Duplicación en `ServerResolverTests` (Standards, DRY, menor)**: `new ServerResolver(cfxService, new ServerRequirementsResolver())` se repite 10×.

## Solution

- **`FiveMLaunchOptions` pasa de `sealed record` a `sealed class`** (decisión confirmada con el usuario): ctor privado + get-only props. Al no ser record no existe `with`, el invariante queda **bloqueado en compilación** (no hay forma de construir estado inválido fuera de `Create`). Se pierde value-equality (no usado por nadie). `ToUri()`/`ToCommandLineArgs()` siguen igual de forma.
- **`ToCommandLineArgs()` devuelve `ImmutableArray<string>`** (BCL de .NET 10, sin dependencia nueva): inmutable de verdad, inmune a cast por índice y a mutadores. Firma pública pasa de `IReadOnlyList<string>` a `ImmutableArray<string>` (add `using System.Collections.Immutable;`).
- **Estilos heredables en `MainView.xaml`**: `DropdownButtonStyle` y `ArrowButtonStyle` usan `BasedOn` sobre un estilo base común (p.ej. `MainButtonStyle` o un nuevo base) y solo declaran sus diferencias; se aprovecha que `SecondaryButtonStyle` ya demuestra el patrón.
- **Botones de ventana en `MainWindow.xaml`**: un `Style` compartido para Minimizar/Cerrar (mismo ancho, alto, fondo, primer plano, borde, cursor y `Template`/chrome) + convertir las dimensiones repetidas en el layout del `WindowChrome` en un recurso estático (`CaptionHeight`/`GridLength`/`Width`).
- **Arrange helper en `ServerResolverTests`**: método privado (o variable local de fábrica) `CreateResolver(cfxService)` que arma `new ServerResolver(cfxService, new ServerRequirementsResolver())`, eliminando la repetición 10×.

## User Stories

1. Como desarrollador, quiero que no exista ninguna forma de construir `FiveMLaunchOptions` con estado inválido (ni `with`, ni ctor público, ni mutadores), para que el invariante esté garantizado por el compilador.
2. Como desarrollador, quiero que `ToCommandLineArgs()` devuelva una colección inmutable de verdad (sin mutación por cast), para que ningún consumidor corrompa los args de lanzamiento.
3. Como desarrollador, quiero que la serialización (`ToUri`/`ToCommandLineArgs`) tenga el mismo comportamiento observable de hoy (formato, negación Enhanced, orden `-b` antes de `-pure_`, `-cl2` condicional), para no romper lo ya verificado.
4. Como mantenimiento, quiero que los estilos de botón de `MainView.xaml` y `MainWindow.xaml` centralicen las propiedades comunes (via `BasedOn`/`Style` compartido), para que un cambio de aspecto toque un solo lugar.
5. Como mantenimiento, quiero que los tests de `ServerResolver` reduzcan la repetición de arrange mediante un helper, sin tocar la lógica probada.

## Implementation Decisions

- **`sealed class` con ctor privado y get-only props** (decisión del usuario, confirmada en conversación). Los `init` se eliminan: props ge-only asignadas en el ctor privado. `Create`/`FromServerProfile` siguen siendo las únicas fábricas públicas.
- **`ImmutableArray<string>`** como tipo de retorno de `ToCommandLineArgs()`. `Array.Empty<string>().ToImmutableArray()` o el builder según el caso; `IsDefaultOrEmpty` no aplica (siempre devolvemos no-default). Tests existentes que hacían `Assert.Equal(new[] { ... }, args)` siguen funcionando (comparación por contenido).
- Las **casillas de los tickets 01 y 03 de `.scratch/fivem-launch-options-review/`** volverán a tomar sentido: el ticket 01 exigía "no inválido fuera de fábrica (bloqueado en diseño/compilación)" que hoy está incumplido, y el 03 "inmutable de verdad". Esta fase cierra ambos; al terminar deben reflejarlo (los tickets ya están `resolved`; el estado real ahora sí coincide con su `[x]`).
- Puerta TDD estricta: cada ticket RED → GREEN → **REFACTOR obligatorio** (DRY/KISS/SOLID/YAGNI) con suite verde, antes del siguiente test (regla de AGENTS.md).
- Los hallazgos Standards 3-5 son DRY/refactor puros: se validan por refactor (suite verde + diff de mejoras) sin tests RED obligatorios, porque no hay cambio de comportamiento — solo se mantiene verde antes y después. Si un refactor XAML/test helper cambia comportamiento observable, el ticket correspondiente añade su test RED.

## Testing Decisions

- **Ticket 01 (sealed class)**: RED — test que compruebe por reflexión que `with` ya no es posible (cero `init` accessors públicos y sin método sintético de clone, test implementado como `Type_ShouldHaveNoMutatingAccessors`) y se ajusta `Value_ShouldBeImmutableAfterConstruction` (ya no hay `with`; verifica ausencia de mutadores y que dos instancias independientes con igual estado exponen exactamente los mismos valores).
- **Ticket 02 (ImmutableArray)**: RED — test que casté a `IList<string>` y escriba por índice (`args[0] = "x"`) esperando que NO mute la colección devuelta (con `string[]` muta/compila; con `ImmutableArray` no hay indexador de escritura públicamente → el test cambia a comprobar inmutabilidad vía `IsDefaultOrEmpty`/contenido estable, y que un segundo llamada devuelve la misma secuencia). Más importante: verificar mediante reflexión o por tipo que el retorno NO es `string[]` (RED sobre el tipo concreto). Vecindad con test existente `ToCommandLineArgs_ShouldReturnReadOnlyList` (Add lanza) → se adapta al nuevo tipo.
- **Ticket 03 (MainView XAML)**: sin test de comportamiento (refactor visual); comprobar que `dotnet build` sigue verde y la UI arranca/mock no cambia visualmente de forma estructural. Si se sospecha pérdida de look, el review posterior lo captura.
- **Ticket 04 (MainWindow XAML)**: ídem, build verde.
- **Ticket 05 (ServerResolver tests)**: RED no aplica; refactor de arrange; suite verde igual.
- Suite completa verde (60 hoy) al final de cada ticket y al cierre del esfuerzo.

## Out of Scope

- **Cambiar el comportamiento de serialización** (formatos, Enhanced, orden `-b`/`-pure_`, `-cl2`): se conserva exactamente igual.
- **Añadir validación en runtime** (`ToUri()`/`ToCommandLineArgs()` re-validando): innecesario una vez el invariante está en compilación.
- **Extraer `ServerRequirements`-y-fábrica en un adaptador con tipo nuevo** (Data Clumps / `Create` 5-arity): se desprioriza por YAGNI hasta que exista un consumidor real de Dev Mode.
- **IP:port/domain resolution**, CitizenFX.ini, Steam/Discord, RSC (TODOs de otras fases).

## Further Notes

- Fuente: code review de `6f67ba8...HEAD`, hallazgos consolidados en conversación; decisión `sealed class` confirmada por el usuario (alternativa descartada: mantener record + revalidar en runtime).
- `ImmutableArray<string>` requiere `using System.Collections.Immutable;`; está en la BCL de .NET 10 (sin NuGet nuevo).
- Tras implementar: actualizar `AGENTS.md` (forma de `FiveMLaunchOptions`, tipo de retorno y conteo de tests), marcar casillas `[x]` y commitear con conventional commits en inglés.