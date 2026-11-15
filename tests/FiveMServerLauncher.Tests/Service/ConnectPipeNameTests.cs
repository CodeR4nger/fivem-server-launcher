using System.Diagnostics;
using System.Security.Principal;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ConnectPipeNameTests
{
    private const string UserA = "S-1-5-21-1000-2000-3000-1001";
    private const string UserB = "S-1-5-21-1000-2000-3000-1002";

    [Fact]
    public void For_WhenSessionsDiffer_ShouldYieldDifferentNames()
    {
        // Given / When: named pipes share one machine-global namespace, so two Windows
        // sessions of the same user must never collide on the name
        var first = ConnectPipeName.For(UserA, sessionId: 1);
        var second = ConnectPipeName.For(UserA, sessionId: 2);

        // Then
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void For_WhenUsersDiffer_ShouldYieldDifferentNames()
    {
        // Given / When
        var first = ConnectPipeName.For(UserA, sessionId: 1);
        var second = ConnectPipeName.For(UserB, sessionId: 1);

        // Then
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void For_WhenInputsAreEqual_ShouldYieldTheSameName()
    {
        // Given / When: both ends derive the name independently
        var first = ConnectPipeName.For(UserA, sessionId: 3);
        var second = ConnectPipeName.For(UserA, sessionId: 3);

        // Then
        Assert.Equal(first, second);
    }

    [Fact]
    public void ForCurrentSession_ShouldEmbedCurrentUserAndSession()
    {
        // Given
        var sid = WindowsIdentity.GetCurrent().User!.Value;
        var sessionId = Process.GetCurrentProcess().SessionId;

        // When
        var name = ConnectPipeName.ForCurrentSession();

        // Then
        Assert.Equal(ConnectPipeName.For(sid, sessionId), name);
    }

    [Fact]
    public void DefaultPipeNames_ShouldMatchBetweenListenerAndForwarder()
    {
        // Given / When
        using var listener = new ConnectRequestListener();
        var forwarder = new ConnectRequestForwarder();

        // Then: the duplicate instance finds the first one only if both derive one name
        Assert.Equal(ConnectPipeName.ForCurrentSession(), listener.PipeName);
        Assert.Equal(listener.PipeName, forwarder.PipeName);
    }
}
