using FiveMServerLauncher.Configuration;

namespace FiveMServerLauncher.Tests.Configuration;

public class PortableDataDirectoryTests
{
    [Fact]
    public void Ctor_WhenProviderNull_ShouldThrow()
    {
        // Given a null process-path provider, When constructing, Then it throws.
        Assert.Throws<ArgumentNullException>(() => new PortableDataDirectory(null!));
    }

    [Theory]
    [InlineData(@"C:\Apps\CFXLauncher.exe", @"C:\Apps")]
    [InlineData(@"C:\Apps\CFXLauncher", @"C:\Apps")]
    public void DataDirectory_ShouldBeExecutableFolder(string processPath, string expected)
    {
        // Given the injected process path is the executable, When resolved, Then the data dir is its folder.
        var sut = new PortableDataDirectory(() => processPath);

        Assert.Equal(expected, sut.DataDirectory);
    }

    [Fact]
    public void SettingsPath_ShouldPointToLauncherSettingsJsonBesideExecutable()
    {
        // Given an executable in a folder, When composing the settings path, Then it is launcher-settings.json beside it.
        var sut = new PortableDataDirectory(() => @"C:\Apps\CFXLauncher.exe");

        Assert.Equal(@"C:\Apps\launcher-settings.json", sut.SettingsPath);
    }

    [Fact]
    public void ServersPath_ShouldPointToSavedServersJsonBesideExecutable()
    {
        // Given an executable in a folder, When composing the servers path, Then it is saved-servers.json beside it.
        var sut = new PortableDataDirectory(() => @"C:\Apps\CFXLauncher.exe");

        Assert.Equal(@"C:\Apps\saved-servers.json", sut.ServersPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DataDirectory_WhenProcessPathNull_ShouldThrow(string? processPath)
    {
        // Given the process path resolves to nothing usable, When resolved, Then it fails fast.
        var sut = new PortableDataDirectory(() => processPath);

        Assert.Throws<InvalidOperationException>(() => sut.DataDirectory);
    }
}