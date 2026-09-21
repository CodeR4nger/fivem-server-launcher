using System.Diagnostics;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public class ProcessReadinessChecker(Func<string, bool>? isProcessRunning = null) : IRequirementReadiness
{
    private readonly Func<string, bool> _isProcessRunning = isProcessRunning ?? DefaultProcessLookup;

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
}