namespace FiveMServerLauncher.Domain;

public interface IDnsResolver
{
    Task<string?> ResolveToIpAsync(string host);
}