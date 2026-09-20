using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Domain;

public class ServerRequirementsResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnGameBuild()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            EnforceGameBuild = 3258,
            GameClient = Core.Enums.GameClient.FiveM,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Equal(3258, result.GameBuild);
    }

}