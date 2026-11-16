using System.Windows;
using System.Windows.Controls;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The text inputs use the theme's TextInputStyle as-is: typed text starts exactly where its
// placeholder sits, and the dialog's fields line their text up with the dialog's combo boxes.
public partial class MainViewModelTests
{
    public static TheoryData<string> PlaceholderInputs() => new()
    {
        "ServerSearchBox",
        "ServerAddressBox",
        "BrowserSearchBox"
    };

    [Theory]
    [MemberData(nameof(PlaceholderInputs))]
    public void PlaceholderInput_ShouldStartTypedTextAtThePlaceholdersLeftEdge(string boxName)
    {
        // Given the real window, the input and its placeholder overlay
        var (textLeft, placeholderLeft) = RunOnSta(() =>
        {
            var (window, _) = boxName == "BrowserSearchBox" ? ShowBrowse("en") : ShowServersPage("en");

            try
            {
                var shell = (MainView)window.FindName("ShellContent");
                var page = (FrameworkElement)shell.FindName(boxName == "BrowserSearchBox" ? "BrowsePage" : "ServersPage");
                var box = (TextBox)page.FindName(boxName);
                var placeholder = ((Panel)box.Parent).Children.OfType<TextBlock>()
                    .Single(t => t.Style == (Style)box.FindResource("PlaceholderTextStyle"));

                // When the empty box shows its placeholder, then text is typed
                box.Text = string.Empty;
                window.UpdateLayout();
                var placeholderLeft = placeholder.TranslatePoint(new Point(0, 0), box).X;

                box.Text = "Wide";
                window.UpdateLayout();

                return (box.GetRectFromCharacterIndex(0).Left, placeholderLeft);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the text starts where the placeholder did
        Assert.Equal(placeholderLeft, textLeft, 1);
    }

    [Fact]
    public void DialogField_ShouldInsetItsTextLikeTheDialogsComboBoxes()
    {
        // Given the server dialog with its loopback section (text fields and combo boxes)
        var (fieldInset, comboInset) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell("en");

            try
            {
                vm.OpenAddServerDialogCommand.Execute(null);
                vm.DialogServerName = "Local";
                vm.DialogServerAddress = "localhost:30120";
                vm.DialogGameClient = GameClient.FiveM;
                window.UpdateLayout();

                var dialog = (FrameworkElement)((MainView)window.FindName("ShellContent")).FindName("ServerDialog");
                var card = (FrameworkElement)dialog.FindName("ServerDialogCard");
                var nameField = Descendants<TextBox>(card).First();
                var gameCombo = Descendants<ComboBox>(card).First();

                // When the name text and the combo's selected text are located
                var comboText = Descendants<TextBlock>(gameCombo).First(t => t.Text == "FiveM");

                return (nameField.GetRectFromCharacterIndex(0).Left,
                    comboText.TranslatePoint(new Point(0, 0), gameCombo).X);
            }
            finally
            {
                window.Close();
            }
        });

        // Then both start the same distance in from their box's edge
        Assert.Equal(comboInset, fieldInset, 1);
    }

    [Fact]
    public void HeaderAndFilterRows_ShouldTabLeftToRight()
    {
        // Given the Servers list header and the Browse filter row, where some controls
        // are docked right
        var (servers, browse) = RunOnSta(() =>
        {
            var (window, _) = ShowServersPage("en");
            var (browseWindow, _) = ShowBrowse("en");

            try
            {
                var page = (FrameworkElement)((MainView)window.FindName("ShellContent")).FindName("ServersPage");
                var header = Ancestor<DockPanel>((FrameworkElement)page.FindName("AddServerButton"));
                var filters = (FrameworkElement)BrowsePage(browseWindow).FindName("BrowseFilters");

                // When their tab stops are listed in tab (tree) order
                return (TabStopLefts(header), TabStopLefts(filters));
            }
            finally
            {
                window.Close();
                browseWindow.Close();
            }
        });

        // Then Tab moves left to right, as the row reads
        Assert.True(servers.Length >= 2 && servers.SequenceEqual(servers.Order()), string.Join(", ", servers));
        Assert.True(browse.Length >= 4 && browse.SequenceEqual(browse.Order()), string.Join(", ", browse));
    }

    // The left edges of a region's keyboard tab stops, in tree order (= Tab order: no
    // TabIndex is set anywhere in these rows).
    private static double[] TabStopLefts(FrameworkElement region) =>
        Descendants<Control>(region)
            .Where(c => c.IsVisible && c.Focusable && c.IsTabStop && c is not ItemsControl || c is ComboBox)
            .Where(c => c.IsVisible)
            .Select(c => Math.Round(c.TranslatePoint(new Point(0, 0), region).X))
            .ToArray();

    private static T Ancestor<T>(DependencyObject element) where T : DependencyObject
    {
        for (var node = System.Windows.Media.VisualTreeHelper.GetParent(element); node is not null;
             node = System.Windows.Media.VisualTreeHelper.GetParent(node))
        {
            if (node is T match)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"no {typeof(T).Name} above {element}");
    }
}
