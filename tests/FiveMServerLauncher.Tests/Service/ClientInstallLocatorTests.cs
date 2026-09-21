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
        public string? InstallDirectoryResult { get; set; } = @"C:\FiveM\FiveM.app";

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

        public Task<string?> GetInstallDirectoryAsync(GameClient client)
        {
            LastQueriedClient = client;
            return Task.FromResult(InstallDirectoryResult);
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

    [Fact]
    public async Task GetExecutablePath_MissingEnhanced_ShouldReturnNullViaRealImpl()
    {
        // Given
        var exists = (string _) => false; // fake file system check: nothing on disk
        var locator = new ClientInstallLocator(exists);

        // When
        var result = await locator.GetExecutablePathAsync(GameClient.FiveMEnhanced);

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task IsInstalled_LegacyNotOnDisk_ShouldReturnFalseViaRealImpl()
    {
        // Given
        var exists = (string _) => false;
        var locator = new ClientInstallLocator(exists);

        // When
        var result = await locator.IsInstalledAsync(GameClient.FiveM);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task GetExecutablePath_LegacyOnDisk_ShouldReturnPathViaRealImpl()
    {
        // Given
        string capturedPath = string.Empty;
        var exists = (string path) =>
        {
            capturedPath = path;
            return true;
        };
        var locator = new ClientInstallLocator(exists);

        // When
        var result = await locator.GetExecutablePathAsync(GameClient.FiveM);

        // Then
        Assert.EndsWith(Path.Combine("FiveM", "FiveM.app", "FiveM.exe"), capturedPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(capturedPath, result);
    }

    [Fact]
    public async Task GetInstallDirectory_LegacyOnDisk_ShouldReturnDirectoryViaRealImpl()
    {
        // Given
        string capturedPath = string.Empty;
        var exists = (string path) =>
        {
            capturedPath = path;
            return true;
        };
        var locator = new ClientInstallLocator(exists);

        // When
        var result = await locator.GetInstallDirectoryAsync(GameClient.FiveM);

        // Then
        Assert.EndsWith(Path.Combine("FiveM", "FiveM.app", "FiveM.exe"), capturedPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("FiveM", "FiveM.app"), result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInstallDirectory_WithMissingLegacy_ShouldReturnNullViaRealImpl()
    {
        // Given
        var exists = (string _) => false;
        var locator = new ClientInstallLocator(exists);

        // When
        var result = await locator.GetInstallDirectoryAsync(GameClient.FiveM);

        // Then
        Assert.Null(result);
    }
}