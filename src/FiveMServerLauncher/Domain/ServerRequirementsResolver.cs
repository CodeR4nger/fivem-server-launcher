using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Domain;

public class ServerRequirementsResolver
{
    public ServerRequirements Resolve(CfxServerInfo serverInfo)
    {
        return new ServerRequirements
        {
            GameBuild = serverInfo.EnforceGameBuild,
            PureMode = serverInfo.PureLevel,
            RequestSteamTicket = serverInfo.RequestSteamTicket,
            DefaultBuild = serverInfo.DefaultGameBuild,
            ReplaceExecutable = serverInfo.ReplaceExecutableToSwitchBuilds,
            PoolSizesIncrease = serverInfo.PoolSizesIncrease,
            SteamRequired = serverInfo.SteamEnforced
        };
    }
}