# 03: App composition root (grafo real)

**What to build:** en `App.xaml.cs`, quitar `StartupUri="MainWindow.xaml"` y cmabiar `OnStartup` para construir el grafo: `HttpClient` → `CfxService` → `ServerCatalog` → `ServerResolver` → `GameLauncher` → `MainViewModel` → `MainWindow` (DataContext) → `Show()`.

**Blocked by:** 01, 02

**Status:** ready-for-agent

- [ ] `App.xaml.cs` `OnStartup` construye grafo manualmente (sin DI container).
- [ ] UI muestra durante la aplicación real.
- [ ] Compila. Suite verde.

## Comments