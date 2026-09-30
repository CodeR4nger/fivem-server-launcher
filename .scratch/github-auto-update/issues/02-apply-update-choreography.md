# 02: Apply an update (download → staged swap → relaunch)

**What to build:** Given a newer release (ticket 01's answer), one operation installs it with no
elevation, fully portable: download the tagged asset to a staged temp file, verify sanity
(expected asset name, non-trivial size), then swap on restart — rename the running exe (allowed
on Windows even while running), move the staged file into its place, relaunch the new exe, and
exit the old one. Every shell step sits behind the existing seam pattern (process starter +
recording fakes); the file steps are verified against a unique temp directory with real I/O
(the amendment recorded in ## Comments). Any failure at any step — download, sanity check,
rename, move, relaunch — leaves the
running exe untouched and reports failure: the user is never left with a broken or missing
launcher. The relaunched instance must not be eaten by the single-instance guard of the
still-exiting old one (the guard is released or bypassed as part of the handoff).

**Blocked by:** 01 (the release info shape: version + asset name/size/URL).

**Status:** resolved

## Answer

Delivered as `Launch/IUpdateApplier` + `UpdateApplier` (Application layer, primary-ctor
orchestrator): `ApplyAsync(LauncherUpdate) → Task<bool>` — true = fully handed off (the caller
does nothing else), false = failed with the running exe untouched and still running.
Dependencies all injectable: `HttpClient` (download — the composition root must supply a
dedicated instance with a generous timeout in ticket 04; the shared 15 s client cannot carry a
~62 MB asset), `Func<string?>` exe-path provider (PortableDataDirectory precedent), the
existing `IProcessStarter` for the relaunch (`UseShellExecute = true`), the single-instance
guard as `IDisposable`, and an exit `Action` (real wiring: `Application.Current.Shutdown`).

Choreography: resolve path → refuse unless the asset name equals the running exe's own file
name (before any download) → best-effort sweep of stale `*.old-*` backups (a locked one never
blocks) → download → sanity (non-empty AND equal to the published size, verified before
anything is written) → stage as `<name>.new-<guid>` beside the exe → rename the running exe to
`<name>.old-<guid>` (allowed on Windows while running) → move the staged file in → dispose the
guard (the mutex is free before the new process can acquire it) → relaunch → exit. Any
failure in the guarded region triggers best-effort rollback that restores the backup
atomically (`File.Replace` when the new file already sits at the exe path — no window without
an exe on disk; `File.Move` when only the rename happened) and removes the staged file.
The exit call sits deliberately AFTER the guarded region: a throwing exit can never roll back
an already-running new instance, and a failure before it never exits the launcher.

Accepted edge, pinned by test: when the relaunch itself fails, the guard was already
released — the launcher keeps running unlocked until its next restart (benign: the mutex only
dedupes windows; worst case a second instance can start in that session).

Review follow-ups applied: the download now streams straight into the staged file (the spec's
"download to a temp file" letter — no ~62 MB in-memory buffer; the sanity check reads the
staged file's length), a failing swap step is pinned by a real locked-exe rename failure
(exercising the rollback's no-backup path; the backup-restore path is pinned by the
relaunch-failure test), the asset-name gate follows Windows case-insensitivity, and the test
constructions share a builder.

Known residuals, deliberate: rollback is best-effort — if the restore itself fails (disk
error/AV lock at the worst moment) the exe path can end up missing while `false` is reported;
retry machinery would fail in the same instant, so ticket 05's real-release E2E is the
coverage for that class. A throwing `exitApplication` propagates to the caller — ticket 03's
UPDATE command must catch around the whole apply. No cancellation token yet — mid-download
window close simply kills the process before any swap; revisit in ticket 03/04 if the
in-progress UX needs a cancel.

Tests (`Launch/UpdateApplierTests`, 9): real temp-dir I/O proving end states — happy-path swap
with handoff-order pin (guard-released → relaunch → exit) and no leftover files; download
failure; truncation/size refusal; misnamed-asset refusal (zero requests); unresolvable path;
relaunch-failure rollback restoring the original bytes; stale-backup sweep; case-only asset
name; locked-exe rename failure.

- [x] The success choreography runs in order — download → stage → sanity-check → rename the
      running exe → move the new exe in → relaunch → old exits — each step observable through
      the fakes
- [x] The staged asset is sanity-checked (expected name, sane size) before any swap happens
- [x] A download failure, sanity failure, or failing swap step performs no partial swap: the
      current exe keeps running and the failure is reported (no broken install, no deleted exe)
- [x] The relaunch targets the new exe's final path through the process seam
- [x] The single-instance handoff is safe: the relaunched instance is not treated as a duplicate
      of the exiting one
- [x] No real network in tests; the file choreography runs against a unique temp directory so
      the tests prove real end states (swap result, rollback) — process, guard and exit through
      recording fakes
- [x] Suite green (RED → GREEN → REFACTOR) — 797 tests green

**Spec:** `.scratch/github-auto-update/spec.md`

## Comments

- Decisions at seam agreement (2026-09-30): file steps tested with real temp-dir I/O (spec's
  "no real files" line amended — end states and rollback on real NTFS are the value; fakes
  would only pin call order); single-instance handoff = the applier releases the guard
  (Dispose) immediately before relaunching, deterministic for the normal case — a user-started
  duplicate in the sub-second window could steal the lock, which is benign (after the swap both
  candidates are the new exe).
- Sanity rule: the update's asset name must equal the running exe's own file name (self-
  consistent, no duplicated constant), and the staged bytes must be non-empty and match the
  published asset size (truncation guard).
