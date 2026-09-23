# v1.1 — Single-instance enforcement

Status: ready-for-agent

## Problem Statement

The launcher can be started multiple times; each process opens its own window with the same
portable data files. Two running instances can clobber each other's settings/saved-servers
writes and confuse the user about "the" launcher.

## Solution

Only one launcher instance may be active. Starting a second copy does not open a new window:
it activates/focuses the existing window if possible, then exits silently.

## User Stories

1. As a user, I want double-clicking the launcher twice to keep one window, so that I never
   juggle duplicate launchers.
2. As a user, I want the second launch to bring my existing window to front (restoring it if
   minimized), so that the second click still feels responsive.
3. As a user, I want no scary error dialog for the second instance, so that accidental
   double-launch is harmless.
4. As a user, I want the lock to be machine-scoped to this product (named mutex), so that it
   never collides with other apps.

## Implementation Decisions

- Enforcement lives at the composition root (`App.OnStartup`): acquire a named system
  `Mutex` (e.g. `Local\CFXLauncher.SingleInstance`) before building the object graph; if
  already held, bring the existing window forward and shut down the new process.
- Bring-to-front: find the existing `MainWindow` via its process/window title through the
  current process list; use `SetForegroundWindow`/`ShowWindow(SW_RESTORE)` via minimal Win32
  interop behind a seam (`IExistingWindowActivator`) so tests never touch real windows. If
  activation fails, still exit quietly.
- Mutex lifetime: held for the app lifetime, released implicitly on exit (abandoned-state
  safe by design since nothing waits on it except the second instance).
- Portable-data safety: the mutex is the same regardless of which exe copy launched (same
  product name), so two copies of the exe in different folders still can't run together —
  deliberate (the data files live beside the exe, but the app is meant to be run from one
  place; document this in the spec/repo docs).

## Testing Decisions

- The seam: `IExistingWindowActivator` (real impl uses Win32; tests use a fake recording
  activations) plus a named-mutex wrapper or a thin `Func<bool>` "try acquire" seam so
  composition-root logic (acquire → activate → exit decision) is testable without real OS
  primitives. Which seam shape wins gets decided at ticket time; prefer the fewest seams.
- Pure-logic tests: given "mutex already held", the app path activates the existing window
  and does not build/show a window; given "mutex free", normal startup proceeds.
- Real OS behavior (mutex + focus) is verified manually, not unit-tested, per repo
  convention for OS seams.
- TDD per repo standard: RED → minimal GREEN → mandatory REFACTOR.

## Out of Scope

- Single-instance per-data-folder semantics; command-line arguments to the second instance;
  IPC to pass addresses between instances; tray behavior.
- Any change to how the running instance behaves.

## Further Notes

- Decided during v1.1 phase 11 work (user request). Single instance is a hard product
  constraint, consistent with the portable single-file distribution.
