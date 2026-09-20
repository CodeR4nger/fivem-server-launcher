Status: resolved
Type: spec

# Process launch failure handling

## Problem Statement

El review `71669ab...HEAD` encontró el issue principal: `Process.Start(...)` puede fallar silenciosamente — la excepción se propaga del hilo de UI sin manejo. El `GameLauncher.ConnectAsync` no cubre ese caso y el usuario queda viendo "Lanzando..." sin éxito.

## Solution

Minimal, sin cambio a seams:

1. `LaunchResult` (sealed hierarchy) gana `StartFailed` como tercero.
2. `GameLauncher.ConnectAsync` captura la excepción de proceso (sin especificar tipo, un `catch (Exception)` en el launcher debe confiar en Process.Start; el dominio no lo filtra aquí) → devuelve `StartFailed`.
3. `MainViewModel.ConnectAsync` switch gana `case LaunchResult.StartFailed => "No se puede lanzar"`.