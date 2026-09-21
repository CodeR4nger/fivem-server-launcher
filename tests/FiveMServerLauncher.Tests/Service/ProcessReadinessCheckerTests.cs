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

    [Fact]
    public async Task IsReady_WhenSteamHelperRunningAndUserSignedIn_ShouldReturnTrue()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name is "steam" or "steamwebhelper",
            () => 1234567);

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.True(result);
    }

    [Fact]
    public async Task IsReady_WhenSteamHelperAbsent_ShouldReturnFalse()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name == "steam",
            () => 1234567);

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task IsReady_WhenSteamProcessAbsent_ShouldReturnFalse()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name == "steamwebhelper",
            () => 1234567);

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task IsReady_WhenNoUserSignedIn_ShouldReturnFalse()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name is "steam" or "steamwebhelper",
            () => 0);

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task IsReady_WhenRegistryValueMissing_ShouldReturnFalse()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name is "steam" or "steamwebhelper",
            () => null);

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }

    [Fact]
    public async Task IsReady_ForDiscord_ShouldFollowProcessPresence()
    {
        // Given
        var checker = new ProcessReadinessChecker(name => name == "Discord");

        // When
        var discordPresent = await checker.IsReadyAsync(ExternalApp.Discord);
        var discordAbsent = await new ProcessReadinessChecker(_ => false).IsReadyAsync(ExternalApp.Discord);

        // Then
        Assert.True(discordPresent);
        Assert.False(discordAbsent);
    }

    [Fact]
    public async Task IsReady_WhenRegistryReadThrows_ShouldReturnFalseAndNotThrow()
    {
        // Given
        var checker = new ProcessReadinessChecker(
            name => name is "steam" or "steamwebhelper",
            () => throw new InvalidOperationException("registry unavailable"));

        // When
        var result = await checker.IsReadyAsync(ExternalApp.Steam);

        // Then
        Assert.False(result);
    }
}
