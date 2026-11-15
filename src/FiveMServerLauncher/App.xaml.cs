using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static string LegacyAppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveMServerLauncher");

    // Set once the window is up; until then an unhandled exception is a failed startup
    // and must still end the process (a handled one would leave it alive with no window).
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

        var singleInstance = new SingleInstanceGuard(
            new MutexSingleInstanceLock(),
            new Win32ExistingWindowActivator());

        if (!singleInstance.TryStart())
        {
            // The duplicate never reaches the view model: the activator already
            // brought the running window forward, so the shortcut's (or the
            // cfxlauncher:// web link's) address —
            // and nothing else — travels the pipe.
            var forwardedAddress = ConnectArgument.TryParse(e.Args);

            if (forwardedAddress is not null)
            {
                new ConnectRequestForwarder().TryForward(forwardedAddress);
            }

            Shutdown();
            return;
        }

        Exit += (_, _) => singleInstance.Dispose();

        var dataDirectory = PortableDataDirectory.Default();
        LegacyDataMigration.Migrate(
            LegacyAppDataDirectory,
            new[] { dataDirectory.SettingsPath, dataDirectory.ServersPath });

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        var catalog = new ServerCatalog(httpClient);
        var resolver = new ServerResolver(
            new CfxService(httpClient),
            catalog,
            new ServerRequirementsResolver(),
            new DnsResolver());
        var installLocator = new ClientInstallLocator();
        var launcher = new GameLauncher(
            new GameProcessLauncher(new ProcessStarter(), new UriSchemeRegistration()),
            new CitizenFxPreparer(installLocator, new CitizenFxConfigWriter()),
            installLocator);

        var readiness = new ProcessReadinessChecker();

        var enrichment = new ServerEnrichmentService(catalog, httpClient);

        var cfxStatus = new CfxStatusService(httpClient);

        var gameBuildData = new GameBuildDataService(httpClient);

        var releaseFeed = new GitHubReleaseFeed(httpClient);

        // The asset download streams tens of megabytes: the shared 15 s client would abort
        // it mid-flight, so the applier gets its own generously timed client. It reuses
        // the single-instance guard (released before the relaunch) and exits through
        // Shutdown once the handoff completes.
        var updateApplier = new UpdateApplier(
            new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            },
            () => Environment.ProcessPath,
            new ProcessStarter(),
            singleInstance,
            Shutdown);

        var localizer = Localizer.FromEmbeddedResources();
        LocalizationSource.Instance.Attach(localizer);

        var refreshCts = new CancellationTokenSource();

        var connectAddress = ConnectArgument.TryParse(e.Args);

        var window = new MainWindow();
        var serverRepository = new FileServerRepository(dataDirectory.ServersPath);
        var viewModel = new MainViewModel(
            resolver,
            launcher,
            serverRepository,
            new ExternalAppPreparer(
                readiness,
                new ExternalAppStarter(new ProcessStarter(), new UriSchemeRegistration())),
            installLocator,
            new ConfigurationRepository(
                new FileSettingsStorage(dataDirectory.SettingsPath)),
            enrichment,
            cfxStatus,
            new ServerBrowserViewModel(catalog, enrichment, serverRepository, localizer: localizer, cancellationToken: refreshCts.Token),
            localizer,
            cancellationToken: refreshCts.Token,
            releaseFeed: releaseFeed,
            updateApplier: updateApplier,
            shortcuts: new ShortcutDependencies(
                new WshShortcutCreator(),
                () => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                () => Environment.ProcessPath,
                new ShortcutIconWriter(() => Path.Combine(
                    Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty,
                    "shortcut-icons"))),
            webLinks: new LauncherSchemeRegistrar(
                new UriSchemeRegistration(),
                () => Environment.ProcessPath),
            systemAnimations: new SystemAnimationSetting());
        window.DataContext = viewModel;
        var trayWindow = new TrayWindowController(new WpfTrayWindow(window), () => viewModel.MinimizeToTray);
        window.Show();
        _viewModel = viewModel;
        window.Dispatcher.InvokeAsync(() => viewModel.InitializeAsync(connectAddress));

        window.Closed += (_, _) => refreshCts.Cancel();

        // A minimized, inactive or tray-hidden window parks the enrichment loop (no idle
        // catalog downloads while the game runs in front); coming back releases it at once.
        void ReportAttendance(object? sender, EventArgs args) =>
            viewModel.SetWindowAttended(trayWindow.IsAttended);
        window.StateChanged += (_, _) => trayWindow.OnStateChanged();
        window.Activated += ReportAttendance;
        window.Deactivated += ReportAttendance;
        window.StateChanged += ReportAttendance;
        window.IsVisibleChanged += (_, _) => ReportAttendance(window, EventArgs.Empty);
        ReportAttendance(window, EventArgs.Empty);

        // A tray-hidden window is only ever shown through WPF (see MainWindowMessages).
        MainWindowMessages.Attach(window, trayWindow);

        // Always present until exit. Every exit path removes it: window close (also the tray
        // Exit and Application.Shutdown, which the update relaunch uses), the Exit event, and
        // a crash or process exit that skips both.
        var tray = new TrayIcon(
            AppInfo.Title,
            viewModel.BuildTrayMenu,
            entry => OnTrayEntrySelected(entry, window, trayWindow, viewModel),
            trayWindow.ShowAndActivate);
        window.Closed += (_, _) => tray.Dispose();
        Exit += (_, _) => tray.Dispose();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => tray.RemoveIcon();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => tray.RemoveIcon();

        window.Dispatcher.InvokeAsync(() => viewModel.RunEnrichmentLoopAsync(refreshCts.Token));
        _ = RefreshSessionGameBuildDataAsync(window, viewModel, gameBuildData);

        // The IPC listener lives for the whole session: a shortcut against the
        // running instance forwards its address through the pipe, and the
        // dispatcher hands it to the same connect flow the browser uses.
        var connectCts = new CancellationTokenSource();
        Exit += (_, _) => connectCts.Cancel();
        var connectListener = new ConnectRequestListener();
        connectListener.Start(
            address => window.Dispatcher.InvokeAsync(() => ShowAndConnectAsync(address, trayWindow, viewModel)),
            connectCts.Token);
    }

    // Forwarded (pipe) and tray quick-connects: bring the window back through WPF so the
    // connect status is visible, then the normal busy-guarded forwarded connect.
    private static async Task ShowAndConnectAsync(
        string? address,
        TrayWindowController trayWindow,
        MainViewModel viewModel)
    {
        try
        {
            trayWindow.ShowAndActivate();
            await viewModel.HandleForwardedConnectAsync(address);
        }
        catch
        {
            // The browser and manual paths ride the same swallow through
            // AsyncRelayCommand; a forwarded connect gets parity.
        }
    }

    private static void OnTrayEntrySelected(
        TrayMenuEntry entry,
        Window window,
        TrayWindowController trayWindow,
        MainViewModel viewModel)
    {
        switch (entry.Kind)
        {
            case TrayMenuEntryKind.Server:
                _ = ShowAndConnectAsync(entry.Address, trayWindow, viewModel);
                break;
            case TrayMenuEntryKind.Show:
            case TrayMenuEntryKind.ShowAll:
                trayWindow.ShowAndActivate();
                break;
            case TrayMenuEntryKind.Exit:
                window.Close();
                break;
        }
    }

    // Last resort: a bug that escaped every guarded path keeps the app running with one
    // generic status line. The tested unit is MainViewModel.ReportUnexpectedError.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_viewModel is not { } viewModel)
        {
            return;
        }

        e.Handled = true;
        viewModel.ReportUnexpectedError();
    }

    private static async Task RefreshSessionGameBuildDataAsync(
        Window window,
        MainViewModel viewModel,
        IGameBuildDataService gameBuildData)
    {
        try
        {
            if (await gameBuildData.FetchAsync() is { } data)
            {
                await window.Dispatcher.InvokeAsync(() => viewModel.SetGameBuildData(data));
            }
        }
        catch (Exception)
        {
        }
    }
}