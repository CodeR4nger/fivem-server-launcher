using System.IO;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Launch;

public interface ICitizenFxPreparer
{
    Task PrimeAsync(ServerProfile profile);
}

public sealed class CitizenFxPreparer(
    IClientInstallLocator installLocator,
    ICitizenFxConfigWriter configWriter) : ICitizenFxPreparer
{
    private readonly IClientInstallLocator _installLocator = installLocator;
    private readonly ICitizenFxConfigWriter _configWriter = configWriter;

    private const string CitizenFxIniFileName = "CitizenFX.ini";

    public async Task PrimeAsync(ServerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var client = profile.GameClient;

        if (!profile.IsCfxValidated || client is not (GameClient.FiveM or GameClient.RedM))
        {
            return;
        }

        var values = BuildValues(profile.Requirements);

        try
        {
            var directory = await _installLocator.GetInstallDirectoryAsync(client.Value);
            if (directory is null)
            {
                return;
            }

            await _configWriter.ApplyAsync(Path.Combine(directory, CitizenFxIniFileName), values);
        }
        catch
        {
        }
    }

    private static Dictionary<string, string> BuildValues(ServerRequirements requirements)
    {
        var values = new Dictionary<string, string>();

        if (requirements.DefaultBuild is int defaultBuild && defaultBuild > 0)
        {
            values["DefaultBuild"] = defaultBuild.ToString();
        }
        else if (requirements.ReplaceExecutable == true && requirements.GameBuild is int gameBuild)
        {
            values["DefaultBuild"] = gameBuild.ToString();
        }

        values["PoolSizesIncrease"] = string.IsNullOrWhiteSpace(requirements.PoolSizesIncrease)
            ? string.Empty
            : requirements.PoolSizesIncrease;

        return values;
    }
}