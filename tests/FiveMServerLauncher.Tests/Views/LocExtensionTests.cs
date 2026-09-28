using System.Windows.Data;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class LocExtensionTests
{
    [Fact]
    public void Ctor_ShouldBindToIndexerOnLocalizationSource()
    {
        // Given / When — the key must target the indexer (a plain property path
        // resolves to nothing and the bound text renders empty).
        var binding = new LocExtension("SettingsButton");

        // Then
        Assert.Equal("[SettingsButton]", binding.Path?.Path);
        Assert.Same(LocalizationSource.Instance, binding.Source);
        Assert.Equal(BindingMode.OneWay, binding.Mode);
    }
}
