namespace FiveMServerLauncher.Service;

// Windows' "show animations in Windows" (client-area animation) setting, behind a seam so
// the view model's effective motion flag is testable. Changed fires when the system value
// may have changed.
public interface ISystemAnimationSetting
{
    bool IsEnabled { get; }

    event EventHandler? Changed;
}
