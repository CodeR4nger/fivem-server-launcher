using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Views;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

public partial class MainViewModelTests
{
    public static TheoryData<string> ShippedLanguageTags()
    {
        var data = new TheoryData<string>();

        foreach (var language in Localizer.ShippedLanguageOptions)
        {
            data.Add(language.Tag);
        }

        return data;
    }

    private static T RunOnSta<T>(Func<T> work) => StaThread.Run(work);
}
