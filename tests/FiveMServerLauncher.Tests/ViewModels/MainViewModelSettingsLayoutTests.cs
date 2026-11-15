using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Launch;
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

    [Theory]
    [MemberData(nameof(ShippedLanguageTags))]
    public void SettingsPanel_ShouldShowEveryCheckboxUnclipped(string languageTag)
    {
        // Given the real window at its fixed size, in the given language, with the
        // settings panel open
        var problems = RunOnSta(() =>
        {
            var localizer = Localizer.FromEmbeddedResources();
            LocalizationSource.Instance.Attach(localizer);
            var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), localizer: localizer);

            // After the view model: it applies the saved language (none = system) on
            // construction, which would otherwise override the language under test.
            localizer.SetLanguage(languageTag);
            var window = new MainWindow
            {
                DataContext = vm,
                ShowInTaskbar = false,
                ShowActivated = false,
                Left = -10000,
                Top = -10000
            };

            try
            {
                window.Show();
                var checkBoxes = FindSettingsCheckBoxes(window);
                var panel = FindSettingsPanel(checkBoxes[0]);

                // The fade-in storyboard drives these when the user opens the panel;
                // local values stand in for the end state of that animation.
                panel.Visibility = Visibility.Visible;
                panel.Opacity = 1;
                window.UpdateLayout();

                // When each checkbox is measured against the panel and the window
                return checkBoxes
                    .SelectMany(checkBox => ClippingProblems(window, panel, checkBox))
                    .ToList();
            }
            finally
            {
                window.Close();
            }
        });

        // Then every checkbox and its whole label are visible
        Assert.Empty(problems);
    }

    private static List<CheckBox> FindSettingsCheckBoxes(DependencyObject root)
    {
        string[] settingsPaths = ["AutoLaunch", "WebLinks", "MinimizeToTray"];
        var found = new List<CheckBox>();

        void Walk(DependencyObject node)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);

                if (child is CheckBox checkBox
                    && checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty)?.ParentBinding.Path.Path
                        is { } path
                    && settingsPaths.Contains(path))
                {
                    found.Add(checkBox);
                }

                Walk(child);
            }
        }

        Walk(root);
        Assert.Equal(settingsPaths.Length, found.Count);
        return found;
    }

    private static Border FindSettingsPanel(DependencyObject checkBox)
    {
        var node = VisualTreeHelper.GetParent(checkBox);

        while (node is not Border { BorderThickness.Left: 1 })
        {
            node = VisualTreeHelper.GetParent(node);
        }

        return (Border)node;
    }

    private static IEnumerable<string> ClippingProblems(Window window, Border panel, CheckBox checkBox)
    {
        var name = $"{checkBox.Content}";
        var inPanel = checkBox.TransformToAncestor(panel)
            .TransformBounds(new Rect(checkBox.RenderSize));

        // The panel draws its own border: a checkbox reaching past it is cut off.
        if (inPanel.Bottom > panel.ActualHeight - panel.Padding.Bottom + 0.5)
        {
            yield return $"'{name}' ends below the panel ({inPanel.Bottom:0} > {panel.ActualHeight - panel.Padding.Bottom:0})";
        }

        // The panel itself must not be cut by its layout slot (the row it lives in).
        var panelInWindow = panel.TransformToAncestor((Visual)window.Content)
            .TransformBounds(new Rect(panel.RenderSize));
        var slot = LayoutInformation.GetLayoutSlot(panel);

        if (panel.ActualHeight > slot.Height + 0.5)
        {
            yield return $"panel is {panel.ActualHeight:0}px tall but its slot is {slot.Height:0}px ('{name}')";
        }

        if (panelInWindow.Bottom > ((FrameworkElement)window.Content).ActualHeight)
        {
            yield return "panel runs past the bottom of the window";
        }

        // The label must fit the checkbox's own width (no text cut on the right).
        var label = FindLabel(checkBox);
        var labelRight = label.TransformToAncestor(checkBox)
            .TransformBounds(new Rect(label.RenderSize)).Right;

        if (labelRight > checkBox.ActualWidth + 0.5 || label.DesiredSize.Width > label.RenderSize.Width + 0.5)
        {
            yield return $"'{name}' label is cut on the right";
        }
    }

    private static TextBlock FindLabel(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is TextBlock textBlock)
            {
                return textBlock;
            }

            if (child is not null && FindLabelOrNull(child) is { } nested)
            {
                return nested;
            }
        }

        throw new InvalidOperationException("Checkbox has no text label.");
    }

    private static TextBlock? FindLabelOrNull(DependencyObject root)
    {
        try
        {
            return FindLabel(root);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
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
