using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class ExternalAppPreparerTests
{
    private static Task NoWait() => Task.CompletedTask;

    [Fact]
    public async Task TryPrepareAsync_WhenAlreadyRunning_ShouldReturnTrueWithoutStarting()
    {
        // Given
        var readiness = new StatefulRequirementReadiness(_ => true);
        var starter = new FakeExternalAppStarter();
        var preparer = new ExternalAppPreparer(readiness, starter, NoWait, maxAttempts: 3);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Steam);

        // Then
        Assert.True(prepared);
        Assert.Empty(starter.Starts);
    }

    [Fact]
    public async Task TryPrepareAsync_WhenStartsAndBecomesRunning_ShouldReturnTrue()
    {
        // Given
        var checks = 0;
        var readiness = new StatefulRequirementReadiness(_ => ++checks >= 3);
        var starter = new FakeExternalAppStarter();
        var preparer = new ExternalAppPreparer(readiness, starter, NoWait, maxAttempts: 10);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Discord);

        // Then
        Assert.True(prepared);
        Assert.Equal([ExternalApp.Discord], starter.Starts);
    }

    [Fact]
    public async Task TryPrepareAsync_WhenNeverRunning_ShouldReturnFalseAfterBoundedAttempts()
    {
        // Given
        var readiness = new StatefulRequirementReadiness(_ => false);
        var starter = new FakeExternalAppStarter();
        var preparer = new ExternalAppPreparer(readiness, starter, NoWait, maxAttempts: 4);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Steam);

        // Then
        Assert.False(prepared);
        Assert.Equal(5, readiness.Checks);
        Assert.Equal([ExternalApp.Steam], starter.Starts);
    }

[Fact]
    public async Task TryPrepareAsync_WhenStartThrows_ShouldReturnFalseAndNeverThrow()
    {
        // Given
        var readiness = new StatefulRequirementReadiness(_ => false);
        var starter = new FakeExternalAppStarter
        {
            ThrowOnStart = new System.ComponentModel.Win32Exception("no association")
        };
        var preparer = new ExternalAppPreparer(readiness, starter, NoWait, maxAttempts: 4);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Steam);

        // Then
        Assert.False(prepared);
        Assert.Empty(starter.Starts);
    }

    [Fact]
    public async Task TryPrepareAsync_WhenReadinessThrows_ShouldReturnFalseAndNeverThrow()
    {
        // Given
        var readiness = new StatefulRequirementReadiness(_ => throw new InvalidOperationException("lookup failed"));
        var starter = new FakeExternalAppStarter();
        var preparer = new ExternalAppPreparer(readiness, starter, NoWait, maxAttempts: 4);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Steam);

        // Then
        Assert.False(prepared);
        Assert.Empty(starter.Starts);
    }

    [Fact]
    public async Task TryPrepareAsync_WhenWaitThrows_ShouldReturnFalseAndNeverThrow()
    {
        // Given
        var readiness = new StatefulRequirementReadiness(_ => false);
        var starter = new FakeExternalAppStarter();
        var preparer = new ExternalAppPreparer(
            readiness,
            starter,
            () => throw new InvalidOperationException("wait failed"),
            maxAttempts: 4);

        // When
        var prepared = await preparer.TryPrepareAsync(ExternalApp.Steam);

        // Then
        Assert.False(prepared);
        Assert.Equal([ExternalApp.Steam], starter.Starts);
    }
}

