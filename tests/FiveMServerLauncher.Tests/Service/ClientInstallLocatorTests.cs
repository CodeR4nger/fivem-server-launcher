using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ClientInstallLocatorTests
{
    private sealed class FakeClientInstallLocator : IClientInstallLocator
    {
        public GameClient? LastQueriedClient { get; private set; }
        public bool IsInstalledResult { get; set; } = true;
        public string? ExecutablePathResult { get; set; } = @"C:\FiveM\FiveM.exe";

        public Task<bool> IsInstalledAsync(GameClient client)
        {
            LastQueriedClient = client;
            return Task.FromResult(IsInstalledResult);
        }

        public Task<string?> GetExecutablePathAsync(GameClient client)
        {
            LastQueriedClient = client;
            return Task.FromResult(ExecutablePathResult);
        }
    }

    [Fact]
    public async Task IsInstalled_WithLegacyClient_ShouldReturnTrue()
    {
        // Given
        IClientInstallLocator locator = new FakeClientInstallLocator { IsInstalledResult = true };

        // When
        var result = await locator.IsInstalledAsync(GameClient.FiveM);

        // Then
        Assert.True(result);
    }

    [Fact]
    public async Task IsInstalled_WithMissingLegacy_ShouldReturnFalse()
    {
        // Given
        IClientInstallLocator locator = new FakeClientInstallLocator { IsInstalledResult = false };

        // When
        var result = await locator.IsInstalledAsync(GameClient.FiveM);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task GetExecutablePath_WithInstalledEnhaced_ShouldReturnPath()
    {
        // Given
        const string expectedPath = @"C:\Users\test\AppData\Local\FiveM for GTAV Enhanced\FiveM.app\FiveM.exe";
        IClientInstallLocator locator = new FakeClientInstallLocator
        {
            ExecutablePathResult = expectedPath
        };

        // When
        var result = await locator.GetExecutablePathAsync(GameClient.FiveMEnhanced);

        // Then
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public async Task GetExecutablePath_WithMissingEnhanced_ShouldReturnNull()
    {
        // Given
        IClientInstallLocator locator = new FakeClientInstallLocator { ExecutablePathResult = null };

        // When
        var result = await locator.GetExecutablePathAsync(GameClient.FiveMEnhanced);

        // Then
        Assert.Null(result);
    }
}