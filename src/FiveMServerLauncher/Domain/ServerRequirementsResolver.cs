using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Domain;

public class ServerRequirementsResolver
{
    public ServerRequirements Resolve(CfxServerInfo serverInfo)
    {
        return new ServerRequirements
        {
            GameBuild = serverInfo.EnforceGameBuild
        };
    }
}