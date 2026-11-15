# Persistence safety

Session 4 of `.scratch/review-hardening/plan.md` (wave 2, first). Makes saved-server and
settings persistence safe against data loss and makes write failures non-fatal. Scope:
`Configuration/FileServerRepository`, `Configuration/FileSettingsStorage`, a new shared
`Configuration/StoreFile` helper + `Configuration/StoreUnavailableException`, the repository
and settings call sites in `ViewModels/MainViewModel`, `App.xaml.cs`, tests, 11 localization
files. All findings come from code reading; each is confirmed by a failing test first.

## Findings

1. `FileServerRepository.GetAll` turns `IOException`/`JsonException` into an empty list and
   every mutator does `GetAll()` → `WriteAll`: one transient read failure (AV sharing lock)
   followed by any mutation overwrites the file with only the new state. An
   `UnauthorizedAccessException` on read is not caught at all (startup crash).
2. Entries failing `IsValid` are filtered on read and therefore dropped permanently by the next
   write.
3. `FileSettingsStorage.Load` has the same read-as-defaults shape: a locked or corrupt
   settings file followed by any setting change is overwritten with defaults.
4. The repository re-reads and re-parses the file on every `GetAll`/`FindBy*` (each connect).
5. Repository/settings writes can throw `IOException`/`UnauthorizedAccessException`
   (read-only exe folder, locked `File.Replace`) and the sync `RelayCommand` paths and
   property setters let them escape to the dispatcher → crash.
6. No last-resort `DispatcherUnhandledException` handler.

## Decisions

### Read outcomes (shared by both stores)

`StoreFile.Read(path)` classifies a read as **Missing** (no file → empty store, writable),
**Unreadable** (`IOException`/`UnauthorizedAccessException` → nothing known about the
content) or **Text** (parsed by the store). A store whose text does not parse (or parses to
JSON `null`) is **Corrupt**: `StoreFile.BackUpCorrupt(path)` copies it to `<path>.bak`
(overwriting an older `.bak`: the newest corrupt content is the one worth recovering — an
older one was already replaced by a valid write in between) and the store starts empty and
writable. If the backup itself fails, the store is treated as Unreadable.

### Never write after an unreadable read

- Servers: an Unreadable read is NOT cached; reads answer empty (the UI still starts), and
  every mutator retries the read first. Still unreadable → `StoreUnavailableException`
  (derives from `IOException`, so existing `IOException` handlers cover it) with the file
  untouched. A retry that succeeds loads and caches normally, then mutates.
- Settings: the VM holds the settings object loaded once at startup, so a later successful
  re-read cannot be merged with an in-memory copy built from defaults. An Unreadable `Load`
  therefore blocks `Save` for the life of the storage instance (`StoreUnavailableException`,
  file untouched) until a later `Load` succeeds. In practice: a settings file locked at
  startup is read-only for that session; changes are refused visibly and reverted.

### Invalid entries are preserved, not dropped

The servers file is parsed as a JSON array of elements. Each element that deserializes to a
valid `SavedServer` joins the in-memory list; every other element (null, `{}`, blank name,
unclassifiable address, wrong field types) is kept verbatim as a raw `JsonElement` and
re-emitted on every write, **after** the valid entries in its original relative order. They are
never shown, so their position carries no meaning; appending keeps `Move` indices equal to
visible indices. A top-level value that is not an array is Corrupt (→ `.bak`).

### In-memory store and locking

After the first successful read the list lives in memory (single-instance app). Every mutator
builds the next list, writes it, and only then commits it to memory — a failed write leaves
memory equal to disk. `GetAll` returns a snapshot array, so a caller iterating it never races
a mutation. All access goes through one private lock: `CaptureCfxIdAsync` writes from a
background continuation (thread pool in tests, UI thread in the app only because WPF
captures the sync context), and a lock is cheap next to the file write it guards. `Move`
stays position-only. `InMemoryServerRepository` mirrors the contract (duplicate `Add`
throws, snapshot reads, settable `WriteFailure` to make mutators throw without changing
state).

### Write failures are non-fatal

`MainViewModel` funnels every store write through one guard catching
`IOException`/`UnauthorizedAccessException` → status `StatusSaveFailed` ("Could not save
changes"), and UI state stays equal to the store:

- delete-confirm: the row stays; reorder/drag: persist-first already, the visible move is
  skipped; browser save: no row, the browser item is not marked saved; save dialog (add,
  same-address update, address-changing replace): the dialog stays open with the inline
  error, no row change, no refresh; `PersistServer` (row Steam/Discord setters — not bound in
  any view today): status only.
- settings setters (`AutoLaunch`, `PreferredClientOption`, `SelectedLanguageOption`,
  `DevGameBuild`, `DevPureMode`): the in-memory value is reverted and PropertyChanged
  re-raised so the binding snaps back; the language is not applied.
- the post-connect `LastServerAddress` save: reverted silently — the launch status wins.
- `CaptureCfxIdAsync` additionally swallows `UnauthorizedAccessException`.
- `RelayCommand` is left unchanged: the guard lives at the call sites (a generic catch in
  the command would hide bugs), and the dispatcher handler is the last resort.

### Startup

The first reads can no longer throw for IO/permission faults (classified above). The
`InitializeAsync` guard catches `UnauthorizedAccessException` as well as `IOException`.
`LegacyDataMigration` already swallows both per copy (pinned by an added test).

### Last-resort handlers

`MainViewModel.ReportUnexpectedError()` sets status `StatusUnexpectedError` ("Something went
wrong"); this is the tested unit. `App.xaml.cs` registers `DispatcherUnhandledException`
(mark handled, report through the VM) and `TaskScheduler.UnobservedTaskException`
(`SetObserved`; no status, it fires at GC time on the finalizer thread). No log file (user
decision).

## Testing

Real temp-dir I/O for the stores (read failures via an injectable `readAllText` seam where a
transient fault is needed, a real `FileShare.None` lock otherwise); VM paths via
`InMemoryServerRepository.WriteFailure` and a throwing settings storage.

## Out of scope / follow-ups

- No startup notice when the servers file is unreadable (the list just looks empty until a
  mutation is refused).
