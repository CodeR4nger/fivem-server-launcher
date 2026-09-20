# 06: AGENTS.md y cierre del esfuerzo

**What to build:** Actualizar `AGENTS.md` al estado real tras esta fase: `FiveMLaunchOptions` es `sealed class` (ctor privado, get-only props — invariante en compilación), `ToCommandLineArgs()` devuelve `ImmutableArray<string>`, se refleja la centralización de estilos en `MainView.xaml`/`MainWindow.xaml` (si se considera digno de mención), y el conteo real de tests verdes en `Current state`. Marcar `[x]` todas las casillas de los tickets de este esfuerzo verificadas contra el código, y commitear la fase con conventional commit en inglés.

**Blocked by:** 01, 02, 03, 04, 05

**Status:** resolved

- [x] `AGENTS.md` describe `FiveMLaunchOptions` como `sealed class` no-`record` con ctor privado e invariante en compilación
- [x] `AGENTS.md` refleja `ToCommandLineArgs(): ImmutableArray<string>` y el conteo real de tests (62)
- [x] Todos los tickets de este esfuerzo quedan con casillas `[x]` verificadas contra el código
- [x] La fase se commitea con conventional commit en inglés (`refactor(domain)` 5c4b77f, `chore(tickets)` 03c68d4)
- [x] La suite completa sigue verde en el punto commiteado (62)

## Comments
- Tickets 01-05 resueltos por TDD/refactor: `sealed class` sin `with`, `ImmutableArray<string>` en `ToCommandLineArgs`, `BasedOn` en styles de MainView, style+recursos en MainWindow, helper de arrange en ServerResolverTests.
- Fase commiteada en 5c4b77f + 03c68d4. Suite 62 verde en el punto commiteado.
