using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class ExternalAppStarterTests
{
    [Fact]
    public async Task StartAsync_ForSteam_ShouldHandSteamUriToWindowsShell()
    {
        // Given
        var processStarter = new FakeProcessStarter();
        var starter = new ExternalAppStarter(processStarter, new FakeUriSchemeRegistration());

        // When
        await starter.StartAsync(ExternalApp.Steam);

        // Then
        var startInfo = Assert.Single(processStarter.Starts);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal("steam:///", startInfo.Arguments);
    }

    [Fact]
    public async Task StartAsync_ForDiscord_ShouldHandDiscordUriToWindowsShell()
    {
        // Given
        var processStarter = new FakeProcessStarter();
        var starter = new ExternalAppStarter(processStarter, new FakeUriSchemeRegistration());

        // When
        await starter.StartAsync(ExternalApp.Discord);

        // Then
        var startInfo = Assert.Single(processStarter.Starts);
        Assert.Equal("explorer.exe", startInfo.FileName);
        Assert.Equal("discord:///", startInfo.Arguments);
    }

    [Fact]
    public async Task StartAsync_WhenSchemeNotRegistered_ShouldThrow()
    {
        // Given
        var starter = new ExternalAppStarter(
            new FakeProcessStarter(),
            new FakeUriSchemeRegistration { IsRegistered = false });

        // When
        var exception = await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(
            () => starter.StartAsync(ExternalApp.Steam));

        // Then
        Assert.Equal("No application is associated with the specified file.", exception.Message);
    }
}