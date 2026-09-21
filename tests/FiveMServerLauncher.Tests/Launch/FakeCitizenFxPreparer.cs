using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeCitizenFxPreparer : ICitizenFxPreparer
{
    public List<ServerProfile> PrimedProfiles { get; } = [];

    public bool Throw { get; set; }

    public Task PrimeAsync(ServerProfile profile)
    {
        if (Throw)
        {
            throw new InvalidOperationException("Simulated priming failure");
        }

        PrimedProfiles.Add(profile);
        return Task.CompletedTask;
    }
}