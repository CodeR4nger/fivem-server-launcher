using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Tests.Domain;

internal sealed class FakeDnsResolver : IDnsResolver
{
    private readonly string? _ip;

    public FakeDnsResolver() { }

    public FakeDnsResolver(string ip) => _ip = ip;

    public bool WasCalled { get; private set; }

    public Task<string?> ResolveToIpAsync(string host)
    {
        WasCalled = true;
        return Task.FromResult(_ip);
    }
}