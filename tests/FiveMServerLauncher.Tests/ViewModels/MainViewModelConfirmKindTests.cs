using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;

namespace FiveMServerLauncher.Tests.ViewModels;

// The confirm overlay knows whether the action it guards destroys data, so the view can draw
// its confirm button in the danger style (delete) or the normal one (shortcut overwrite).
public partial class MainViewModelTests
{
    [Fact]
    public void DeleteServerCommand_ShouldAskForADestructiveConfirmation()
    {
        // Given a saved server
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/abc123"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);

        // When its delete is requested
        vm.DeleteServerCommand.Execute(vm.SavedServers[0]);

        // Then the confirmation is marked destructive
        Assert.True(vm.IsConfirmOpen);
        Assert.True(vm.IsConfirmDestructive);
    }

    [Fact]
    public void RequestConfirmation_AfterADestructiveOne_ShouldNotBeDestructive()
    {
        // Given a destructive confirmation that was cancelled
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        vm.RequestDestructiveConfirmation("Delete?", () => { });
        vm.CancelConfirmCommand.Execute(null);

        // When an ordinary confirmation follows
        vm.RequestConfirmation("Overwrite?", () => { });

        // Then it is not drawn as destructive
        Assert.True(vm.IsConfirmOpen);
        Assert.False(vm.IsConfirmDestructive);
    }

    [Fact]
    public void CreateDesktopShortcutCommand_WhenShortcutExists_ShouldAskForANonDestructiveConfirmation()
    {
        // Given a same-named shortcut already on the desktop
        using var desktop = new TempSettingsDirectory();
        System.IO.Directory.CreateDirectory(desktop.DirectoryPath);
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(desktop.DirectoryPath, "My Server.lnk"), "previous");
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/abc123"));
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            shortcutCreator: new FakeShortcutCreator(),
            desktopPathProvider: () => desktop.DirectoryPath,
            targetExePathProvider: () => @"C:\Games\CFXLauncher.exe");

        // When a shortcut is created for the server
        vm.CreateDesktopShortcutCommand.Execute(vm.SavedServers[0]);

        // Then the overwrite question is an ordinary one
        Assert.True(vm.IsConfirmOpen);
        Assert.False(vm.IsConfirmDestructive);
    }
}
