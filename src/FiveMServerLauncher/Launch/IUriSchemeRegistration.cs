namespace FiveMServerLauncher.Launch;

/// <summary>
/// The registry seam for URI schemes: whether a scheme (e.g. <c>fivem://</c>) resolves for
/// this user at all, plus the per-user registration the launcher owns for its own
/// <c>cfxlauncher://</c> scheme (<c>HKCU\Software\Classes</c>). The decisions about the
/// per-user registration live in <see cref="LauncherSchemeRegistrar"/>.
/// </summary>
public interface IUriSchemeRegistration
{
    bool IsSchemeRegistered(string scheme);

    /// <summary>The per-user <c>shell\open\command</c> of the scheme, or null when absent.</summary>
    string? GetUserOpenCommand(string scheme);

    void RegisterForUser(string scheme, string openCommand, string iconPath);

    void UnregisterForUser(string scheme);
}
