Status: resolved
Type: spec

# Process launch failure handling

## Problem Statement

The `71669ab...HEAD` review found the main issue: `Process.Start(...)` can fail silently — the exception propagates off the UI thread unhandled. `GameLauncher.ConnectAsync` doesn't cover that case and the user is left staring at "Lanzando..." with no success.

## Solution

Minimal, no seam changes:

1. `LaunchResult` (sealed hierarchy) gains `StartFailed` as a third case.
2. `GameLauncher.ConnectAsync` catches the process exception (without specifying a type, a `catch (Exception)` in the launcher must trust Process.Start; the domain doesn't filter it here) → returns `StartFailed`.
3. `MainViewModel.ConnectAsync` switch gains `case LaunchResult.StartFailed => "No se puede lanzar"`.
