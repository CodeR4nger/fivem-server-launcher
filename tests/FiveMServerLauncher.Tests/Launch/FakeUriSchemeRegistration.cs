using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeUriSchemeRegistration : IUriSchemeRegistration
{
    public bool IsRegistered { get; set; } = true;

    public bool IsSchemeRegistered(string scheme) => IsRegistered;

    /// <summary>The per-user registrations: scheme → open command.</summary>
    public Dictionary<string, string> UserCommands { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> UserIcons { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int Writes { get; private set; }

    public int Removals { get; private set; }

    /// <summary>When set, every per-user registry call throws it.</summary>
    public Exception? Failure { get; set; }

    public string? GetUserOpenCommand(string scheme)
    {
        ThrowIfFailing();
        return UserCommands.GetValueOrDefault(scheme);
    }

    public void RegisterForUser(string scheme, string openCommand, string iconPath)
    {
        ThrowIfFailing();
        UserCommands[scheme] = openCommand;
        UserIcons[scheme] = iconPath;
        Writes++;
    }

    public void UnregisterForUser(string scheme)
    {
        ThrowIfFailing();
        UserCommands.Remove(scheme);
        UserIcons.Remove(scheme);
        Removals++;
    }

    private void ThrowIfFailing()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
