using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Tests;

public class AppInfoTests
{
    [Fact]
    public void ProductName_ShouldBeCfxLauncher()
    {
        // Given AppInfo is the user-visible product name source, When read, Then it is "CFX Launcher".
        Assert.Equal("CFX Launcher", AppInfo.ProductName);
    }

    [Fact]
    public void Author_ShouldBeCodeRanger()
    {
        // Given AppInfo carries the author credit, When read, Then it is "CodeRanger".
        Assert.Equal("CodeRanger", AppInfo.Author);
    }

    [Fact]
    public void Title_ShouldPinExactWindowTitle()
    {
        // Given the window title and footer bind to AppInfo.Title, When read, Then it is the exact pinned string.
        Assert.Equal("CFX Launcher by CodeRanger", AppInfo.Title);
    }
}