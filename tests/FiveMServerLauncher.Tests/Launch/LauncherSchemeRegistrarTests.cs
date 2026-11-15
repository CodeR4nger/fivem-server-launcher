using System.IO;
using System.Security;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Launch;
using Xunit;

namespace FiveMServerLauncher.Tests.Launch;

public class LauncherSchemeRegistrarTests
{
    private const string Exe = @"D:\Games\CFX\CFXLauncher.exe";
    private const string OurCommand = "\"" + Exe + "\" \"%1\"";

    private readonly FakeUriSchemeRegistration _registry = new();

    private LauncherSchemeRegistrar Registrar(string? exe = Exe) => new(_registry, () => exe);

    [Fact]
    public void TryEnable_WhenSchemeIsFree_ShouldRegisterTheRunningExe()
    {
        // Given / When
        var enabled = Registrar().TryEnable();

        // Then
        Assert.True(enabled);
        Assert.Equal(OurCommand, _registry.UserCommands[LauncherUri.Scheme]);
        Assert.Equal("\"" + Exe + "\",0", _registry.UserIcons[LauncherUri.Scheme]);
    }

    [Fact]
    public void TryEnable_WhenAlreadyRegisteredAtThisPath_ShouldNotRewrite()
    {
        // Given
        _registry.UserCommands[LauncherUri.Scheme] = OurCommand;

        // When
        var enabled = Registrar().TryEnable();

        // Then
        Assert.True(enabled);
        Assert.Equal(0, _registry.Writes);
    }

    [Theory]
    [InlineData(@"""C:\Old\Place\CFXLauncher.exe"" ""%1""")]
    [InlineData(@"""C:\Old\Place\cfxlauncher.EXE"" ""%1""")]
    [InlineData(@"C:\Old\CFXLauncher.exe %1")]
    public void TryEnable_WhenOurRegistrationPointsElsewhere_ShouldRewriteIt(string movedCommand)
    {
        // Given: the exe moved, or another copy registered last
        _registry.UserCommands[LauncherUri.Scheme] = movedCommand;

        // When
        var enabled = Registrar().TryEnable();

        // Then
        Assert.True(enabled);
        Assert.Equal(OurCommand, _registry.UserCommands[LauncherUri.Scheme]);
    }

    [Theory]
    [InlineData(@"""C:\Other\OtherApp.exe"" ""%1""")]
    [InlineData(@"""C:\Other\CFXLauncher.exe.bak"" ""%1""")]
    [InlineData("rundll32.exe url.dll,FileProtocolHandler %1")]
    public void TryEnable_WhenAnotherProgramOwnsTheScheme_ShouldRefuseAndLeaveIt(string foreignCommand)
    {
        // Given
        _registry.UserCommands[LauncherUri.Scheme] = foreignCommand;

        // When
        var enabled = Registrar().TryEnable();

        // Then
        Assert.False(enabled);
        Assert.Equal(foreignCommand, _registry.UserCommands[LauncherUri.Scheme]);
        Assert.Equal(0, _registry.Writes);
    }

    [Fact]
    public void TryEnable_WhenExePathIsUnknown_ShouldFailWithoutWriting()
    {
        // Given / When
        var enabled = Registrar(exe: null).TryEnable();

        // Then
        Assert.False(enabled);
        Assert.Equal(0, _registry.Writes);
    }

    public static TheoryData<Exception> RegistryFaults => new()
    {
        new UnauthorizedAccessException(),
        new IOException(),
        new SecurityException(),
    };

    [Theory]
    [MemberData(nameof(RegistryFaults))]
    public void TryEnable_WhenRegistryFails_ShouldReturnFalse(Exception fault)
    {
        // Given
        _registry.Failure = fault;

        // When
        var enabled = Registrar().TryEnable();

        // Then
        Assert.False(enabled);
    }

    [Theory]
    [InlineData(OurCommand)]
    [InlineData(@"""E:\Another\Copy\CFXLauncher.exe"" ""%1""")]
    public void TryDisable_WhenRegistrationIsOurs_ShouldRemoveIt(string command)
    {
        // Given: ours, from this copy or another one
        _registry.UserCommands[LauncherUri.Scheme] = command;

        // When
        var disabled = Registrar().TryDisable();

        // Then
        Assert.True(disabled);
        Assert.False(_registry.UserCommands.ContainsKey(LauncherUri.Scheme));
    }

    [Fact]
    public void TryDisable_WhenAnotherProgramOwnsTheScheme_ShouldLeaveItAndSucceed()
    {
        // Given
        const string foreign = @"""C:\Other\OtherApp.exe"" ""%1""";
        _registry.UserCommands[LauncherUri.Scheme] = foreign;

        // When
        var disabled = Registrar().TryDisable();

        // Then: the launcher is not registered, which is what "off" means
        Assert.True(disabled);
        Assert.Equal(foreign, _registry.UserCommands[LauncherUri.Scheme]);
        Assert.Equal(0, _registry.Removals);
    }

    [Fact]
    public void TryDisable_WhenNothingIsRegistered_ShouldSucceedWithoutRemoving()
    {
        // Given / When
        var disabled = Registrar().TryDisable();

        // Then
        Assert.True(disabled);
        Assert.Equal(0, _registry.Removals);
    }

    [Theory]
    [MemberData(nameof(RegistryFaults))]
    public void TryDisable_WhenRegistryFails_ShouldReturnFalse(Exception fault)
    {
        // Given
        _registry.UserCommands[LauncherUri.Scheme] = OurCommand;
        _registry.Failure = fault;

        // When
        var disabled = Registrar().TryDisable();

        // Then
        Assert.False(disabled);
    }

    [Fact]
    public void TryDisable_WhenExePathIsUnknown_ShouldFailWithoutRemoving()
    {
        // Given
        _registry.UserCommands[LauncherUri.Scheme] = OurCommand;

        // When
        var disabled = Registrar(exe: null).TryDisable();

        // Then
        Assert.False(disabled);
        Assert.Equal(0, _registry.Removals);
    }
}
