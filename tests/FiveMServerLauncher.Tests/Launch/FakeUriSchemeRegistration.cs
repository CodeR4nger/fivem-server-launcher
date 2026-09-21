using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeUriSchemeRegistration : IUriSchemeRegistration
{
    public bool IsRegistered { get; set; } = true;

    public bool IsSchemeRegistered(string scheme) => IsRegistered;
}