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
}
