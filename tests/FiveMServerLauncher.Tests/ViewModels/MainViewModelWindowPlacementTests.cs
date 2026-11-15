using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;

namespace FiveMServerLauncher.Tests.ViewModels;

// Window size, position and maximized state: remembered in the launcher settings (the
// window saves it on close and on state changes; the startup rules restore it).
public partial class MainViewModelTests
{
    [Fact]
    public void WindowPlacement_ShouldStartFromTheSavedSetting()
    {
        // Given
        var placement = new WindowPlacement(10, 20, 1100, 700, true);
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { Window = placement });

        // When
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), settings: new ConfigurationRepository(storage));

        // Then
        Assert.Equal(placement, vm.WindowPlacement);
    }

    [Fact]
    public void WindowPlacement_WhenSet_ShouldPersistImmediately()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), settings: new ConfigurationRepository(storage));
        var placement = new WindowPlacement(-1200, 40, 1280, 720, false);
        var saves = storage.SaveCount;

        // When
        vm.WindowPlacement = placement;

        // Then
        Assert.Equal(saves + 1, storage.SaveCount);
        Assert.Equal(placement, new ConfigurationRepository(storage).Load().Window);
    }

    [Fact]
    public void WindowPlacement_WhenUnchanged_ShouldNotWriteAgain()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), settings: new ConfigurationRepository(storage));
        vm.WindowPlacement = new WindowPlacement(10, 20, 1100, 700, false);
        var saves = storage.SaveCount;

        // When — an equal value (records compare by value)
        vm.WindowPlacement = new WindowPlacement(10, 20, 1100, 700, false);

        // Then
        Assert.Equal(saves, storage.SaveCount);
    }
}
