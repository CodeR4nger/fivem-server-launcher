using System.Diagnostics;
using FiveMServerLauncher.Core.Enums;
using Microsoft.Win32;

namespace FiveMServerLauncher.Service;

public class ProcessReadinessChecker : IRequirementReadiness
{
    private const string ActiveSteamUserKeyPath = @"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess";
    private const string ActiveSteamUserValueName = "ActiveUser";
    private const string SteamWebHelperProcessName = "steamwebhelper";

    private readonly Func<string, bool> _isProcessRunning;
    private readonly Func<int?> _readActiveSteamUser;

    public ProcessReadinessChecker(
        Func<string, bool>? isProcessRunning = null,
        Func<int?>? readActiveSteamUser = null)
    {
        _isProcessRunning = isProcessRunning ?? DefaultProcessLookup;
        _readActiveSteamUser = readActiveSteamUser ?? ReadActiveSteamUser;
    }

    public Task<bool> IsRunningAsync(ExternalApp app)
    {
        var processName = ProcessNameOf(app);

        try
        {
            if (processName is null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_isProcessRunning(processName));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> IsReadyAsync(ExternalApp app)
    {
        try
        {
            return app switch
            {
                ExternalApp.Steam => Task.FromResult(
                    _isProcessRunning(ProcessNameOf(ExternalApp.Steam)!)
                        && _isProcessRunning(SteamWebHelperProcessName)
                        && _readActiveSteamUser() > 0),
                ExternalApp.Discord => IsRunningAsync(ExternalApp.Discord),
                _ => Task.FromResult(false)
            };
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string? ProcessNameOf(ExternalApp app)
    {
        return app switch
        {
            ExternalApp.Steam => "steam",
            ExternalApp.Discord => "Discord",
            _ => null
        };
    }

    private static bool DefaultProcessLookup(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }

    private static int? ReadActiveSteamUser()
    {
        var value = Registry.GetValue(ActiveSteamUserKeyPath, ActiveSteamUserValueName, null);
        return value is int activeUser ? activeUser : null;
    }
}