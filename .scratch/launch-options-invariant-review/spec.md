Status: ready-for-agent
Type: spec

# Closing code-review findings — FiveMLaunchOptions invariants and DRY in UI/tests

## Problem Statement

The code review of `6f67ba8...HEAD` (31 commits) left two Spec-axis findings that violate acceptance criteria already marked as resolved, and three Standards-axis ones (judgement calls). Summary:

1. **Invariant bypassable with `with` (Spec, high)**: `FiveMLaunchOptions` is a `sealed record` with `init` props and a private ctor. `options with { Address = "garbage" }` or `{ GameClient = (GameClient)99 }` builds invalid state without going through `Create`; `ToUri()` would silently serialize it as `fivem://connect/garbage`. This violates `01-endurecer-validacion-factory.md:13` ("It is not possible to construct `FiveMLaunchOptions` with invalid state outside the factory (blocked at design/compile time)") and "No invalid state is serialized silently".
2. **`ToCommandLineArgs` is not truly immutable (Spec, high)**: it returns `string[]` (`args.ToArray()`); `((IList<string>)args)[0] = "x"` mutates by index. The current test only covers that `Add` throws. Violates `03-serializaciones-robustas.md:9` ("truly immutable collection (not mutable via cast)").
3. **Duplication in `MainView.xaml` (Standards, DRY)**: `DropdownButtonStyle` and `ArrowButtonStyle` re-declare ~9 shared setters that `SecondaryButtonStyle` shows can be inherited with `BasedOn`.
4. **Duplication in `MainWindow.xaml` (Standards, DRY)**: the Minimize/Close buttons repeat the same set of attributes; `Height="40"` recurs across caption/row/buttons.
5. **Duplication in `ServerResolverTests` (Standards, DRY, minor)**: `new ServerResolver(cfxService, new ServerRequirementsResolver())` is repeated 10×.

## Solution

- **`FiveMLaunchOptions` moves from `sealed record` to `sealed class`** (decision confirmed with the user): private ctor + get-only props. Not being a record, `with` does not exist; the invariant is **blocked at compile time** (no way to build invalid state outside `Create`). Value-equality is lost (unused by anyone). `ToUri()`/`ToCommandLineArgs()` stay the same in form.
- **`ToCommandLineArgs()` returns `ImmutableArray<string>`** (.NET 10 BCL, no new dependency): truly immutable, immune to index-cast and mutators. Public signature changes from `IReadOnlyList<string>` to `ImmutableArray<string>` (add `using System.Collections.Immutable;`).
- **Inheritable styles in `MainView.xaml`**: `DropdownButtonStyle` and `ArrowButtonStyle` use `BasedOn` on a common base style (e.g. `MainButtonStyle` or a new base) and only declare their differences; takes advantage of `SecondaryButtonStyle` already demonstrating the pattern.
- **Window buttons in `MainWindow.xaml`**: a shared `Style` for Minimize/Close (same width, height, background, foreground, border, cursor and `Template`/chrome) + turn the repeated dimensions in the `WindowChrome` layout into static resources (`CaptionHeight`/`GridLength`/`Width`).
- **Arrange helper in `ServerResolverTests`**: a private method (or local factory variable) `CreateResolver(cfxService)` that builds `new ServerResolver(cfxService, new ServerRequirementsResolver())`, eliminating the 10× repetition.

## User Stories

1. As a developer, I want there to be no way to build `FiveMLaunchOptions` with invalid state (no `with`, no public ctor, no mutators), so the invariant is guaranteed by the compiler.
2. As a developer, I want `ToCommandLineArgs()` to return a truly immutable collection (no mutation via cast), so no consumer corrupts the launch args.
3. As a developer, I want the serialization (`ToUri`/`ToCommandLineArgs`) to have the same observable behavior as today (format, Enhanced negation, `-b` before `-pure_` order, conditional `-cl2`), so nothing already verified breaks.
4. As maintenance, I want the `MainView.xaml` and `MainWindow.xaml` button styles to centralize common properties (via `BasedOn`/shared `Style`), so an appearance change touches a single place.
5. As maintenance, I want the `ServerResolver` tests to reduce arrange repetition via a helper, without touching the tested logic.

## Implementation Decisions

- **`sealed class` with private ctor and get-only props** (user decision, confirmed in conversation). The `init`s are removed: get-only props assigned in the private ctor. `Create`/`FromServerProfile` remain the only public factories.
- **`ImmutableArray<string>`** as the return type of `ToCommandLineArgs()`. `Array.Empty<string>().ToImmutableArray()` or the builder depending on the case; `IsDefaultOrEmpty` does not apply (we always return non-default). Existing tests doing `Assert.Equal(new[] { ... }, args)` keep working (content comparison).
- The **checkboxes of tickets 01 and 03 in `.scratch/fivem-launch-options-review/`** will make sense again: ticket 01 required "no invalid state outside the factory (blocked at design/compile time)" which is currently unmet, and 03 required "truly immutable". This phase closes both; when done they must reflect it (the tickets are already `resolved`; the real state now actually matches their `[x]`).
- Strict TDD gate: each ticket RED → GREEN → **mandatory REFACTOR** (DRY/KISS/SOLID/YAGNI) with green suite, before the next test (AGENTS.md rule).
- Standards findings 3-5 are pure DRY/refactors: they are validated by refactor (green suite + improvement diff) without mandatory RED tests, because there is no behavior change — it just stays green before and after. If a XAML/test-helper refactor changes observable behavior, the corresponding ticket adds its RED test.

## Testing Decisions

- **Ticket 01 (sealed class)**: RED — a test that verifies via reflection that `with` is no longer possible (zero public `init` accessors and no synthetic clone method, test implemented as `Type_ShouldHaveNoMutatingAccessors`) and adjusts `Value_ShouldBeImmutableAfterConstruction` (no more `with`; verifies absence of mutators and that two independent instances with equal state expose exactly the same values).
- **Ticket 02 (ImmutableArray)**: RED — a test that casts to `IList<string>` and writes by index (`args[0] = "x"`) expecting it NOT to mutate the returned collection (with `string[]` it mutates/compiles; with `ImmutableArray` there is no publicly writable indexer → the test changes to check immutability via `IsDefaultOrEmpty`/stable content, and that a second call returns the same sequence). More important: verify via reflection or by type that the return is NOT `string[]` (RED on the concrete type). Neighbor of the existing `ToCommandLineArgs_ShouldReturnReadOnlyList` test (Add throws) → adapted to the new type.
- **Ticket 03 (MainView XAML)**: no behavior test (visual refactor); check that `dotnet build` stays green and the UI starts/the mock does not change structurally in appearance. If a look regression is suspected, the later review catches it.
- **Ticket 04 (MainWindow XAML)**: ditto, green build.
- **Ticket 05 (ServerResolver tests)**: RED not applicable; arrange refactor; suite equally green.
- Full suite green (60 today) at the end of each ticket and at the close of the effort.

## Out of Scope

- **Changing serialization behavior** (formats, Enhanced, `-b`/`-pure_` order, `-cl2`): kept exactly the same.
- **Adding runtime validation** (`ToUri()`/`ToCommandLineArgs()` re-validating): unnecessary once the invariant is at compile time.
- **Extracting `ServerRequirements`-and-factory into an adapter with a new type** (Data Clumps / `Create` 5-arity): deprioritized per YAGNI until a real Dev Mode consumer exists.
- **IP:port/domain resolution**, CitizenFX.ini, Steam/Discord, RSC (TODOs of other phases).

## Further Notes

- Source: code review of `6f67ba8...HEAD`, findings consolidated in conversation; `sealed class` decision confirmed by the user (discarded alternative: keep record + revalidate at runtime).
- `ImmutableArray<string>` requires `using System.Collections.Immutable;`; it is in the .NET 10 BCL (no new NuGet).
- After implementing: update `AGENTS.md` (`FiveMLaunchOptions` shape, return type and test count), mark `[x]` boxes and commit with English conventional commits.
