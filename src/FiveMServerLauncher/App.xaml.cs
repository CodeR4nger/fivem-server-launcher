using System.IO;
using System.Net.Http;
using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var singleInstance = new SingleInstanceGuard(
            new MutexSingleInstanceLock(),
            new Win32ExistingWindowActivator());

        if (!singleInstance.TryStart())
        {
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
            shortcutCreator: new WshShortcutCreator(),
            desktopPathProvider: () => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            targetExePathProvider: () => Environment.ProcessPath);
        window.DataContext = viewModel;
        window.Show();
        window.Dispatcher.InvokeAsync(() => viewModel.InitializeAsync(connectAddress));

        window.Closed += (_, _) => refreshCts.Cancel();
        window.Dispatcher.InvokeAsync(() => viewModel.RunEnrichmentLoopAsync(refreshCts.Token));
        _ = RefreshSessionGameBuildDataAsync(window, viewModel, gameBuildData);
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