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
    private static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveMServerLauncher");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var httpClient = new HttpClient();
        var resolver = new ServerResolver(
            new CfxService(httpClient),
            new ServerCatalog(httpClient),
            new ServerRequirementsResolver(),
            new DnsResolver());
        var installLocator = new ClientInstallLocator();
        var launcher = new GameLauncher(
            new GameProcessLauncher(new ProcessStarter(), new UriSchemeRegistration()),
            new CitizenFxPreparer(installLocator, new CitizenFxConfigWriter()),
            installLocator);

        var readiness = new ProcessReadinessChecker();

        var window = new MainWindow();
        var viewModel = new MainViewModel(
            resolver,
            launcher,
            new FileServerRepository(Path.Combine(AppDataDirectory, "saved-servers.json")),
            new ExternalAppPreparer(
                readiness,
                new ExternalAppStarter(new ProcessStarter(), new UriSchemeRegistration())),
            installLocator,
            new ConfigurationRepository(
                new FileSettingsStorage(Path.Combine(AppDataDirectory, "launcher-settings.json"))));
        window.DataContext = viewModel;
        window.Show();
        window.Dispatcher.InvokeAsync(viewModel.InitializeAsync);
    }
}