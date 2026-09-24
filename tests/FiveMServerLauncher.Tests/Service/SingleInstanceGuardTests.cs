using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class SingleInstanceGuardTests
{
    [Fact]
    public void TryStart_WhenMutexFree_ShouldAllowStartupWithoutActivation()
    {
        // Given
        var activator = new FakeExistingWindowActivator();
        var guard = new SingleInstanceGuard(new FakeSingleInstanceLock(true), activator);

        // When
        var allowed = guard.TryStart();

        // Then
        Assert.True(allowed);
        Assert.False(activator.WasCalled);
    }

    [Fact]
    public void TryStart_WhenMutexHeld_ShouldActivateExistingWindowAndAbort()
    {
        // Given
        var activator = new FakeExistingWindowActivator();
        var guard = new SingleInstanceGuard(new FakeSingleInstanceLock(false), activator);

        // When
        var allowed = guard.TryStart();

        // Then
        Assert.False(allowed);
        Assert.True(activator.WasCalled);
    }

    [Fact]
    public void TryStart_WhenMutexHeldAndActivationFails_ShouldStillAbortWithoutThrowing()
    {
        // Given
        var activator = new FakeExistingWindowActivator { ShouldThrow = true };
        var guard = new SingleInstanceGuard(new FakeSingleInstanceLock(false), activator);

        // When
        var allowed = guard.TryStart();

        // Then
        Assert.False(allowed);
        Assert.True(activator.WasCalled);
    }

    private sealed class FakeSingleInstanceLock(bool acquired) : ISingleInstanceLock
    {
        public bool TryAcquire() => acquired;

        public void Dispose()
        {
        }
    }

    private sealed class FakeExistingWindowActivator : IExistingWindowActivator
    {
        public bool WasCalled { get; private set; }

        public bool ShouldThrow { get; set; }

        public void ActivateExisting()
        {
            WasCalled = true;

            if (ShouldThrow)
            {
                throw new InvalidOperationException("window not found");
            }
        }
    }
}
