using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class GameProcessLauncherTests
{
    [Fact]
    public async Task StartAsync_ShouldHandUriToWindowsShell()
    {
        // Given
        var processStarter = new FakeProcessStarter();
        var launcher = new GameProcessLauncher(processStarter, new FakeUriSchemeRegistration());
        var uri = new Uri("fivem://connect/cfx.re/join/y4lg95");

        // When
        await launcher.StartAsync(uri);

        // Then
        var startInfo = Assert.Single(processStarter.Starts);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal(uri.AbsoluteUri, startInfo.Arguments);
    }

    [Fact]
    public async Task StartExecutableAsync_ShouldHandExeToWindowsShell()
    {
        // Given
        var processStarter = new FakeProcessStarter();
        var launcher = new GameProcessLauncher(processStarter, new FakeUriSchemeRegistration());

        // When
        await launcher.StartExecutableAsync(@"C:\FiveM\FiveM.app\FiveM.exe");

        // Then
        var startInfo = Assert.Single(processStarter.Starts);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal(@"""C:\FiveM\FiveM.app\FiveM.exe""", startInfo.Arguments);
    }

    [Fact]
    public async Task StartAsync_WhenProtocolNotRegistered_ShouldThrow()
    {
        // Given
        var launcher = new GameProcessLauncher(
            new FakeProcessStarter(),
            new FakeUriSchemeRegistration { IsRegistered = false });
        var uri = new Uri("fivem://connect/cfx.re/join/y4lg95");

        // When
        var exception = await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(() => launcher.StartAsync(uri));

        // Then
        Assert.Equal("No application is associated with the specified file.", exception.Message);
    }
}