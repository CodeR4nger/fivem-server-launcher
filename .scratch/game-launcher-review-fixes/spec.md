Status: resolved
Type: spec

# Fixes de post-review de la fase game-launcher

## Problem Statement

El code review de la fase `game-launcher` (diff `8e7b60d...HEAD`, ejes Standards y Spec) encontró dos hallazgos que apuntan al mismo punto:

1. **Standards:** `LaunchResult` es una null-union: dos payloads nullable (`ConnectUri`/`GameClient`) cuya "kind" se infiere por cuál no es null. Un estado ilegal (ambos null o ambos set) es representable y el reader no ve el contrato del tipo.
2. **Spec:** la spec pedía "`LaunchResult`: tipo inmutable con `Kind` (`Connect | OpenClient`) y payload"; la implementación no tiene discriminador legitimo, solo payloads nulables.

Además, la spec tenía una línea ambigua: "PreferredClient fallback del perfil" — el fallback actual, `?? GameClient.FiveM`, no consulta `LauncherSettings.PreferredClient`. El comportamiento actual es válido; se documenta como decisión: el PreferredClient llegará cuando la UI conecte `LauncherSettings` (fase posterior).

## Solution

- Refactor `LaunchResult` a sealed record hierarchy: `Connect(Uri) : LaunchResult` y `OpenClient(GameClient) : LaunchResult`. El discriminador es el tipo runtime (pattern matching); no hace falta un enum `Kind` separado. Sin ctor público sobre la clase base. Factories estáticas (o records públicos) permiten crear instancias pero los dos estados no son confundibles.
- La spec se aclara: "fallback de `GameClient` es `FiveM` por defecto; `LauncherSettings.PreferredClient` se leerá cuando la UI conecte `ConfigurationRepository`" (fase posterior, fuera de alcance aquí).
- Se renombra el test `ConnectAsync_WithValidationCfxProfile` → `..._WithValidatedCfxProfile`.

## Testing Decisions

- TDD/adaptación: los tests de `GameLauncher` cambian la forma de la assertion con la nueva jerarquía (pattern matching via `IsType`). No hay comportamiento nuevo que validar — solo refactoring de representación.
- Suite completa verde (106) al final.