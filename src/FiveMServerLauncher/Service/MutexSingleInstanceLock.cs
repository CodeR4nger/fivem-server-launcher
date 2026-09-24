using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public sealed class MutexSingleInstanceLock : ISingleInstanceLock
{
    private readonly Mutex _mutex = new(false, $@"Local\{AppInfo.ProductName}.SingleInstance");

    public bool TryAcquire()
    {
        try
        {
            return _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // The previous owner crashed: the mutex is ours.
            return true;
        }
    }

    public void Dispose()
    {
        _mutex.Dispose();
    }
}
