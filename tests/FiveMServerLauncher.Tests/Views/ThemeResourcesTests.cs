using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class ThemeResourcesTests
{
    [Fact]
    public void ThemeResources_ShouldHaveTrueParameterlessConstructor()
    {
        // Given/When XAML instantiates the theme the way BAML does (no arguments)
        // Then construction succeeds — an all-optional-parameter ctor would not
        var instances = RunOnSta(() => new[]
        {
            Activator.CreateInstance(typeof(ThemeResources)),
            Activator.CreateInstance(typeof(Theme))
        });

        Assert.All(instances, Assert.NotNull);
    }

    [Fact]
    public void ThemeResources_ShouldMergeOneSharedThemePerThread()
    {
        // Given two controls on the same UI thread, each merging the theme
        var (first, second) = RunOnSta(() => (new ThemeResources(), new ThemeResources()));

        // Then both merge the very same theme instance (the theme is loaded once)
        var theme = Assert.Single(first.MergedDictionaries);
        Assert.Same(theme, Assert.Single(second.MergedDictionaries));
        Assert.IsType<Theme>(theme);
    }

    [Fact]
    public void ThemeResources_ShouldResolveSharedKeys()
    {
        // Given the theme a control merges
        var found = RunOnSta(() =>
        {
            var resources = new ThemeResources();
            string[] keys = ["AccentBrush", "MainButtonStyle", "PanelFadeIn", "BoolToVisibility"];
            return keys.Select(key => resources.Contains(key)).ToArray();
        });

        // Then every shared key resolves through it
        Assert.All(found, Assert.True);
    }

    private static T RunOnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("STA work failed", failure);
        }

        return result;
    }
}
