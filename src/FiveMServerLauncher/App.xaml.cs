using System.IO;
using System.Net.Http;
using System.Windows;
using FiveMServerLauncher.Configuration;
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

        var localizer = Localizer.FromEmbeddedResources();
        LocalizationSource.Instance.Attach(localizer);

        var refreshCts = new CancellationTokenSource();

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
            cancellationToken: refreshCts.Token);
        window.DataContext = viewModel;
        window.Show();
        window.Dispatcher.InvokeAsync(viewModel.InitializeAsync);

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