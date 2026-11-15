using System.IO;
using System.Security;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Launch;

/// <summary>
/// Decides the per-user <c>cfxlauncher://</c> registration. A registration is ours when its
/// open command's executable has the running exe's file name (<c>CFXLauncher.exe</c>, any
/// folder — another copy of the launcher counts); anything else belongs to another program
/// and is never overwritten nor removed. Never throws: registry faults answer false.
/// </summary>
public sealed class LauncherSchemeRegistrar(
    IUriSchemeRegistration registration,
    Func<string?> exePathProvider)
{
    /// <summary>
    /// Registers the running exe unless another program owns the scheme. Also the startup
    /// refresh: a registration that already points here is left untouched, one pointing at a
    /// moved exe (or another copy) is rewritten.
    /// </summary>
    public bool TryEnable()
    {
        return Run((exe, registered) =>
        {
            if (registered is not null && !IsOurs(registered, exe))
            {
                return false;
            }

            var command = OpenCommandFor(exe);

            if (registered != command)
            {
                registration.RegisterForUser(LauncherUri.Scheme, command, $"\"{exe}\",0");
            }

            return true;
        });
    }

    /// <summary>Removes the registration when it is ours; a foreign one is left alone.</summary>
    public bool TryDisable()
    {
        return Run((exe, registered) =>
        {
            if (registered is not null && IsOurs(registered, exe))
            {
                registration.UnregisterForUser(LauncherUri.Scheme);
            }

            return true;
        });
    }

    private bool Run(Func<string, string?, bool> decide)
    {
        if (exePathProvider() is not { } exe)
        {
            return false;
        }

        try
        {
            return decide(exe, registration.GetUserOpenCommand(LauncherUri.Scheme));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            return false;
        }
    }

    private static string OpenCommandFor(string exe) => $"\"{exe}\" \"%1\"";

    private static bool IsOurs(string command, string exe)
    {
        return string.Equals(
            Path.GetFileName(CommandExecutable(command)),
            Path.GetFileName(exe),
            StringComparison.OrdinalIgnoreCase);
    }

    // The first quoted token, or the first whitespace-separated one when unquoted.
    private static string CommandExecutable(string command)
    {
        var trimmed = command.TrimStart();

        if (trimmed.StartsWith('"'))
        {
            var close = trimmed.IndexOf('"', 1);
            return close < 0 ? trimmed[1..] : trimmed[1..close];
        }

        var space = trimmed.IndexOfAny([' ', '\t']);
        return space < 0 ? trimmed : trimmed[..space];
    }
}
