using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class FakeSystemAnimationSetting : ISystemAnimationSetting
{
    public bool IsEnabled { get; set; } = true;

    public event EventHandler? Changed;

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
