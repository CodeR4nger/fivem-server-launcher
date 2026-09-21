using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.ViewModels;

public sealed class InstalledClientOption(GameClient client)
{
    public GameClient Client { get; } = client;

    public string DisplayName { get; } = DisplayNameOf(client);

    public static string DisplayNameOf(GameClient client)
    {
        return client switch
        {
            GameClient.FiveM => "FiveM",
            GameClient.FiveMEnhanced => "FiveM Enhanced",
            GameClient.RedM => "RedM",
            _ => client.ToString(),
        };
    }
}