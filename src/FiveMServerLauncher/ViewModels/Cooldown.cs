namespace FiveMServerLauncher.ViewModels;

internal static class Cooldown
{
    // Waits out a cooldown so manual refresh commands stay disabled; on the shutdown
    // token the wait ends early and the caller skips its remaining UI work.
    public static async Task<bool> ElapseAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
