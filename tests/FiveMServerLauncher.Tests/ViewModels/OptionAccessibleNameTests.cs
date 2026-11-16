using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// Option objects bound as list, segment or combo items: WPF names an item for UI Automation
// (screen readers) by its ToString(), so each must read as its visible label, never as a
// type name or a record dump.
public class OptionAccessibleNameTests
{
    public static TheoryData<object, string> Options() => new()
    {
        { new InstalledClientOption(GameClient.FiveMEnhanced), "FiveM Enhanced" },
        { new GameClientOption(GameClient.RedM, "RedM"), "RedM" },
        { new LanguageSettingOption("es", "Español"), "Español" },
        { new GameBuildOption(3095, "[3095] The Chop Shop"), "[3095] The Chop Shop" },
        { new ServerBrowserViewModel.SortOption("Name A–Z", ServerBrowserSort.NameAscending), "Name A–Z" },
        { new ServerBrowserViewModel.GameFilterOption("All games", null), "All games" }
    };

    [Theory]
    [MemberData(nameof(Options))]
    public void ToString_ShouldBeTheVisibleLabel(object option, string label)
    {
        // Given / When / Then
        Assert.Equal(label, option.ToString());
    }
}
