using System.ComponentModel;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// Interface animations: the persisted Default/Minimal preference and the effective
// "motion enabled" flag (Default AND Windows' client-area animation setting on).
public partial class MainViewModelTests
{
    [Fact]
    public async Task SetInterfaceAnimations_ShouldPersistImmediately()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        vm.InterfaceAnimations = InterfaceAnimations.Minimal;

        // Then
        Assert.Equal(InterfaceAnimations.Minimal, vm.InterfaceAnimations);
        Assert.Equal(InterfaceAnimations.Minimal, new ConfigurationRepository(storage).Load().InterfaceAnimations);
    }

    [Fact]
    public void InterfaceAnimations_ShouldStartFromTheSavedSetting()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { InterfaceAnimations = InterfaceAnimations.Minimal });

        // When
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));

        // Then
        Assert.Equal(InterfaceAnimations.Minimal, vm.InterfaceAnimations);
        Assert.False(vm.IsMotionEnabled);
    }

    [Theory]
    [InlineData(InterfaceAnimations.Default, true, true)]
    [InlineData(InterfaceAnimations.Default, false, false)]
    [InlineData(InterfaceAnimations.Minimal, true, false)]
    [InlineData(InterfaceAnimations.Minimal, false, false)]
    public void IsMotionEnabled_ShouldRequireDefaultAndWindowsAnimations(
        InterfaceAnimations preference, bool windowsAnimations, bool expected)
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { InterfaceAnimations = preference });
        var system = new FakeSystemAnimationSetting { IsEnabled = windowsAnimations };

        // When
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage),
            systemAnimations: system);

        // Then
        Assert.Equal(expected, vm.IsMotionEnabled);
    }

    [Fact]
    public void IsMotionEnabled_WhenSystemSeamUnwired_ShouldFollowThePreferenceOnly()
    {
        // Given / When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // Then
        Assert.Equal(InterfaceAnimations.Default, vm.InterfaceAnimations);
        Assert.True(vm.IsMotionEnabled);
    }

    [Fact]
    public void SetInterfaceAnimations_ShouldRaiseIsMotionEnabledChanged()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.InterfaceAnimations = InterfaceAnimations.Minimal;

        // Then
        Assert.False(vm.IsMotionEnabled);
        Assert.Contains(nameof(MainViewModel.InterfaceAnimations), raised);
        Assert.Contains(nameof(MainViewModel.IsMotionEnabled), raised);
    }

    [Fact]
    public void WindowsAnimationSettingChange_ShouldUpdateIsMotionEnabled()
    {
        // Given
        var system = new FakeSystemAnimationSetting { IsEnabled = true };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), systemAnimations: system);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        system.IsEnabled = false;
        system.RaiseChanged();

        // Then
        Assert.False(vm.IsMotionEnabled);
        Assert.Contains(nameof(MainViewModel.IsMotionEnabled), raised);
    }

    [Fact]
    public void SetInterfaceAnimations_WhenSaveFails_ShouldKeepMotionFlagConsistent()
    {
        // Given
        var storage = new ThrowingSettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        storage.SaveFailure = new IOException("locked");

        // When
        vm.InterfaceAnimations = InterfaceAnimations.Minimal;

        // Then the reverted preference still drives the motion flag
        Assert.Equal(InterfaceAnimations.Default, vm.InterfaceAnimations);
        Assert.True(vm.IsMotionEnabled);
    }
}
