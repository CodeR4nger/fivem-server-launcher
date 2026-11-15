using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// A pasted address often carries a trailing space or newline; the connect boundary trims
// it once, for the saved-server lookup and the resolve alike.
public partial class MainViewModelTests
{
    [Theory]
    [InlineData("cfx.re/join/y4lg95 ")]
    [InlineData("  cfx.re/join/y4lg95")]
    [InlineData("cfx.re/join/y4lg95\r\n")]
    [InlineData("\ty4lg95\n")]
    public async Task ConnectAsync_WhenAddressHasSurroundingWhitespace_ShouldConnectTrimmed(string pasted)
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        vm.ServerAddress = pasted;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95", Assert.Single(processLauncher.Requests).AbsoluteUri);
    }

    [Fact]
    public async Task ConnectAsync_WhenAddressHasWhitespace_ShouldFindSavedServerByTrimmedValue()
    {
        // Given — the saved row's direct address is only reachable through the trimmed id.
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Local Dev", "localhost:30120", cfxId: "y4lg95"));
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(
            processLauncher,
            "{}",
            repository: repository,
            cfxHttpClient: new HttpClient(new FakeHttpMessageHandler(true)));
        vm.ServerAddress = "y4lg95\n";

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("fivem://connect/localhost:30120", Assert.Single(processLauncher.Requests).AbsoluteUri);
    }

    [Fact]
    public async Task HandleForwardedConnectAsync_WhenAddressHasTrailingNewline_ShouldConnectTrimmed()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));

        // When
        await vm.HandleForwardedConnectAsync("cfx.re/join/y4lg95\r\n");

        // Then
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95", Assert.Single(processLauncher.Requests).AbsoluteUri);
    }

    [Fact]
    public async Task InitializeAsync_WhenConnectArgumentHasWhitespace_ShouldConnectTrimmed()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));

        // When
        await vm.InitializeAsync(" cfx.re/join/y4lg95 ");

        // Then
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95", Assert.Single(processLauncher.Requests).AbsoluteUri);
    }
}
