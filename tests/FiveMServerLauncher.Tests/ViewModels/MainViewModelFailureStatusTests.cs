using FiveMServerLauncher.Tests.Launch;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// An unexpected exception must never leave a flow's in-progress status ("Resolving...") on
// screen: it ends with the last-resort line, and the UI is usable again.
public partial class MainViewModelTests
{
    private const string UnexpectedError = "Something went wrong";

    [Fact]
    public async Task ConnectAsync_WhenUnexpectedFailure_ShouldEndWithUnexpectedErrorStatus()
    {
        // Given — a fault outside the sanctioned outage set escapes the CFX lookup
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            cfxHttpClient: new HttpClient(new FaultingHandler()));
        vm.ServerAddress = "cfx.re/join/abc123";

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal(UnexpectedError, vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task InitializeAsync_WhenStartupConnectFailsUnexpectedly_ShouldEndWithUnexpectedErrorStatus()
    {
        // Given — the startup connect calls ConnectAsync directly, not through a command
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            cfxHttpClient: new HttpClient(new FaultingHandler()));

        // When
        await vm.InitializeAsync("cfx.re/join/abc123");

        // Then
        Assert.Equal(UnexpectedError, vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void Command_WhenExecutionThrows_ShouldReportTheUnexpectedErrorStatus()
    {
        // Given — a command body that throws (a wrong parameter type is the simplest fault)
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // When — Execute still never throws
        var failure = Record.Exception(() => vm.ConnectBrowserServerCommand.Execute("not a browser row"));

        // Then
        Assert.Null(failure);
        Assert.Equal(UnexpectedError, vm.StatusText);
    }

    private sealed class FaultingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("unexpected fault");
        }
    }
}
