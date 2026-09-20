Status: resolved
Type: spec

# English migration

## Problem Statement

Everything, including UI messages, should be English:

- **Code Spanish strings:** `StatusText` Spanish ("Listo", "Resolviendo...", "Lanzando FiveM...", "Dirección inválida", "No se puede lanzar", `Abriendo FiveMEnhanced`), XAML comments (Spanish), ASCII art from DOC.md already in code.
- **Docs:** `DOC.md` (Spanish, the global design ref), its conversion to English.
- **Closed .scratch tickets/specs** hold mixed text (ES + EN); rewrite them as EN only.
- Internal documentation in AGENTS.md is already mostly English — polish any remaining Spanish.

## Solution

Full migration to English in a single pass (code comments and messages, DOC.md docs, historical .scratch tickets and specs). Conventional commits were already English; no change there.

## Implementation Plan

1. **UI strings** (Spanish to English): `MainViewModel.ConnectAsync` switch, `MainViewModel` ctor default, test helpers.
2. `DOC.md` rewrite in English: keep the design decision history.
3. **Spec/ticket rewrites:** browse `.scratch/*` and write specs as EN (replace ES, keep tickets).
4. **File comments/xaml** (existing Spanish comments) swapped to EN comments in code and XAML files.

No behavior change; green suite unaffected because asserts compare text. 117 suite green.