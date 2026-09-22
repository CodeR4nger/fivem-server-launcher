using System.IO;
using System.Net.Http;
using System.Windows;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.ViewModels;

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

        var dataDirectory = PortableDataDirectory.Default();
        LegacyDataMigration.Migrate(
            LegacyAppDataDirectory,
            new[] { dataDirectory.SettingsPath, dataDirectory.ServersPath });

        var httpClient = new HttpClient();
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

        var window = new MainWindow();
        var viewModel = new MainViewModel(
            resolver,
            launcher,
            new FileServerRepository(dataDirectory.ServersPath),
            new ExternalAppPreparer(
                readiness,
                new ExternalAppStarter(new ProcessStarter(), new UriSchemeRegistration())),
            installLocator,
            new ConfigurationRepository(
                new FileSettingsStorage(dataDirectory.SettingsPath)),
            enrichment,
            cfxStatus);
        window.DataContext = viewModel;
        window.Show();
        window.Dispatcher.InvokeAsync(viewModel.InitializeAsync);

        var refreshCts = new CancellationTokenSource();
        window.Closed += (_, _) => refreshCts.Cancel();
        window.Dispatcher.InvokeAsync(() => viewModel.RunEnrichmentLoopAsync(refreshCts.Token));
    }
}