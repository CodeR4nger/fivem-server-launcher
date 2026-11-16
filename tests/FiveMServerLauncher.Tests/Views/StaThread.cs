using System.Windows.Threading;

namespace FiveMServerLauncher.Tests.Views;

// Runs WPF work on a fresh STA thread (one at a time), shuts its dispatcher down, and
// rethrows its failure.
internal static class StaThread
{
    private static readonly object Gate = new();

    // Generous: a layout test takes well under a second even on a loaded machine.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

    public static T Run<T>(Func<T> work, TimeSpan? timeout = null)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                // Destroy every HWND this thread's dispatcher owns (a window or a popup the
                // work left open) while the runtime still runs the thread: Windows destroys
                // leftovers during thread teardown, calling their managed window procedure
                // after the CLR has torn the thread down, which kills the test host and
                // silently ends the run.
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        // A stuck thread must never keep the test process alive.
        thread.IsBackground = true;

        // One WPF thread at a time, process-wide: the pack:// resource package every BAML
        // load reads is not thread-safe, and xUnit runs test classes in parallel.
        lock (Gate)
        {
            thread.Start();

            // A stuck STA test fails by name and releases the gate instead of stalling the
            // whole run silently.
            if (!thread.Join(timeout ?? DefaultTimeout))
            {
                throw new TimeoutException(
                    $"STA work did not finish within {(timeout ?? DefaultTimeout).TotalSeconds:0.#} s");
            }
        }

        if (failure is not null)
        {
            throw new InvalidOperationException("STA work failed", failure);
        }

        return result;
    }

    // Lets the animation clock tick: Begin applies at once, but clock control operations
    // (RemoveStoryboard, Stop) take effect on the next tick.
    public static void Pump(int milliseconds = 60)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}
