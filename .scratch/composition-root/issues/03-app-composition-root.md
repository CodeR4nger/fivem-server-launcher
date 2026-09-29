# 03: App composition root (real graph)

**What to build:** in `App.xaml.cs`, remove `StartupUri="MainWindow.xaml"` and change `OnStartup` to build the graph: `HttpClient` → `CfxService` → `ServerCatalog` → `ServerResolver` → `GameLauncher` → `MainViewModel` → `MainWindow` (DataContext) → `Show()`.

**Blocked by:** 01, 02

**Status:** resolved

- [x] `App.xaml.cs` `OnStartup` builds the graph manually (no DI container).
- [x] UI shows during the real application.
- [x] Compiles. Suite green.

## Comments
