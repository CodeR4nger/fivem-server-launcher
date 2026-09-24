namespace FiveMServerLauncher.Service;

public interface ISingleInstanceLock : IDisposable
{
    bool TryAcquire();
}

public interface IExistingWindowActivator
{
    void ActivateExisting();
}

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly ISingleInstanceLock _lock;
    private readonly IExistingWindowActivator _activator;

    public SingleInstanceGuard(ISingleInstanceLock singleInstanceLock, IExistingWindowActivator activator)
    {
        _lock = singleInstanceLock;
        _activator = activator;
    }

    /// <summary>
    /// true when this process may continue startup; false when another instance owns the
    /// app (its window was asked to come forward and this process must exit silently).
    /// </summary>
    public bool TryStart()
    {
        if (_lock.TryAcquire())
        {
            return true;
        }

        try
        {
            _activator.ActivateExisting();
        }
        catch (Exception)
        {
            // Bringing the existing window forward is best-effort; the duplicate exits regardless.
        }

        return false;
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
