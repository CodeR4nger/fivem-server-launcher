using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ProcessReadinessCheckerTests
{
    [Fact]
    public async Task IsRunning_WhenSteamProcessPresent_ShouldReturnTrue()
    {
        // Given
        var checker = new ProcessReadinessChecker(name => name == "steam");

        // When
        var result = await checker.IsRunningAsync(ExternalApp.Steam);

        // Then
        Assert.True(result);
    }

    [Fact]
    public async Task IsRunning_WhenSteamProcessAbsent_ShouldReturnFalse()
    {
        // Given
        var checker = new ProcessReadinessChecker(_ => false);

        // When
        var result = await checker.IsRunningAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task IsRunning_WhenDiscordProcessPresent_ShouldReturnTrue()
    {
        // Given
        var checker = new ProcessReadinessChecker(name => name == "Discord");

        // When
        var result = await checker.IsRunningAsync(ExternalApp.Discord);

        // Then
        Assert.True(result);
    }

    [Fact]
    public async Task IsRunning_ShouldPassTheAppProcessNameToTheCheck()
    {
        // Given
        string? receivedName = null;
        var checker = new ProcessReadinessChecker(name =>
        {
            receivedName = name;
            return true;
        });

        // When
        await checker.IsRunningAsync(ExternalApp.Steam);

        // Then
        Assert.Equal("steam", receivedName);
    }

    [Fact]
    public async Task IsRunning_WhenCheckThrows_ShouldReturnFalseAndNotThrow()
    {
        // Given
        var checker = new ProcessReadinessChecker(_ => throw new InvalidOperationException("boom"));

        // When
        var result = await checker.IsRunningAsync(ExternalApp.Discord);

        // Then
        Assert.False(result);
    }
}