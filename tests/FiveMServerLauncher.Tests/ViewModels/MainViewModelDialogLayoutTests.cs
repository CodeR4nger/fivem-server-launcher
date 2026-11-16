using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Views;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The modals at the window's minimum size (1000×640), in every shipped language: the card
// sits inside the overlay area (never under the title bar or the footer), and nothing in it
// is cut. A body taller than the card scrolls instead of clipping.
public partial class MainViewModelTests
{
    public enum DialogCase
    {
        AddServer,
        LoopbackWithError,
        ConfirmDelete
    }

    public static TheoryData<string, DialogCase> ShippedLanguagesTimesDialogs()
    {
        var data = new TheoryData<string, DialogCase>();

        foreach (var language in Localizer.ShippedLanguageOptions)
        {
            foreach (var dialog in Enum.GetValues<DialogCase>())
            {
                data.Add(language.Tag, dialog);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedLanguagesTimesDialogs))]
    public void Dialog_AtMinimumSize_ShouldFitTheOverlayAndClipNothing(string languageTag, DialogCase dialog)
    {
        // Given the real window at its minimum size, in the given language, with a saved server
        var problems = RunOnSta(() =>
        {
            var repository = new InMemoryServerRepository();
            repository.Add(SavedServer.Create("A rather long saved server name for the confirmation", "cfx.re/join/abc123"));
            var (window, vm) = ShowShell(languageTag, repository);

            try
            {
                // When the dialog is opened in the given state
                var shell = (MainView)window.FindName("ShellContent");
                var (overlay, cardName, loopbackShown) = OpenDialog(vm, shell, dialog);
                window.UpdateLayout();

                var card = (FrameworkElement)overlay.FindName(cardName);
                var problems = new List<string>();

                if (!card.IsVisible)
                {
                    problems.Add($"{cardName} is not shown");
                    return problems;
                }

                problems.AddRange(CardPlacementProblems(window, overlay, card));
                problems.AddRange(CardContentProblems(card));

                if (loopbackShown is { } expected)
                {
                    var section = (FrameworkElement)overlay.FindName("LoopbackSection");

                    if (section.IsVisible != expected)
                    {
                        problems.Add($"the loopback section visible={section.IsVisible}, expected {expected}");
                    }
                }

                return problems;
            }
            finally
            {
                window.Close();
            }
        });

        // Then the card is whole and inside the overlay
        Assert.Empty(problems);
    }

    private static (FrameworkElement Overlay, string CardName, bool? LoopbackShown) OpenDialog(
        FiveMServerLauncher.ViewModels.MainViewModel vm, MainView shell, DialogCase dialog)
    {
        switch (dialog)
        {
            case DialogCase.AddServer:
                vm.OpenAddServerDialogCommand.Execute(null);
                return ((FrameworkElement)shell.FindName("ServerDialog"), "ServerDialogCard", false);

            case DialogCase.LoopbackWithError:
                vm.OpenAddServerDialogCommand.Execute(null);
                vm.DialogServerAddress = "localhost:30120";
                vm.DialogGameClient = GameClient.FiveM;
                // An empty name fails validation and shows the inline error.
                vm.SaveServerDialogCommand.Execute(null);
                StaThread.Pump();
                return ((FrameworkElement)shell.FindName("ServerDialog"), "ServerDialogCard", true);

            default:
                vm.DeleteServerCommand.Execute(vm.SavedServers[0]);
                return ((FrameworkElement)shell.FindName("ConfirmDialog"), "ConfirmDialogCard", null);
        }
    }

    // The card lies inside the overlay (which spans the content panel and the bottom bar,
    // below the title bar and above the footer), and the overlay inside the window.
    private static IEnumerable<string> CardPlacementProblems(Window window, FrameworkElement overlay, FrameworkElement card)
    {
        var root = (FrameworkElement)window.Content;
        var overlayBounds = overlay.TransformToAncestor(root).TransformBounds(new Rect(overlay.RenderSize));
        var cardBounds = card.TransformToAncestor(root).TransformBounds(new Rect(card.RenderSize));

        if (!Contains(new Rect(root.RenderSize), overlayBounds))
        {
            yield return $"the overlay reaches outside the window ({overlayBounds:0})";
        }

        if (!Contains(overlayBounds, cardBounds))
        {
            yield return $"the card ({cardBounds:0}) reaches outside the overlay ({overlayBounds:0})";
        }

        var slot = LayoutInformation.GetLayoutSlot(card);

        if (card.RenderSize.Height > slot.Height - card.Margin.Top - card.Margin.Bottom + 0.5
            || card.RenderSize.Width > slot.Width - card.Margin.Left - card.Margin.Right + 0.5)
        {
            yield return $"the card is {card.RenderSize.Width:0}×{card.RenderSize.Height:0} in a {slot.Width:0}×{slot.Height:0} slot";
        }
    }

    // Every element is whole in its slot and every text fits its box. Elements inside a
    // ScrollViewer are measured against the scrolled content (it may extend past the
    // viewport: that is what scrolling is for); everything else against the card.
    private static IEnumerable<string> CardContentProblems(FrameworkElement card)
    {
        foreach (var (element, frame) in VisibleDescendantsWithFrame(card, card))
        {
            var label = Describe(element);
            var slot = LayoutInformation.GetLayoutSlot(element);
            var room = new Size(
                Math.Max(0, slot.Width - element.Margin.Left - element.Margin.Right),
                Math.Max(0, slot.Height - element.Margin.Top - element.Margin.Bottom));

            if (element.RenderSize.Width > room.Width + 0.5 || element.RenderSize.Height > room.Height + 0.5)
            {
                yield return $"{label} is {element.RenderSize.Width:0}×{element.RenderSize.Height:0} in a {room.Width:0}×{room.Height:0} slot";
            }

            if (element is TextBlock { TextWrapping: TextWrapping.NoWrap } text
                && NaturalWidth(text) > text.RenderSize.Width - text.Padding.Left - text.Padding.Right + 0.5)
            {
                yield return $"text '{text.Text}' is cut ({NaturalWidth(text):0} > {text.RenderSize.Width:0})";
            }

            var bounds = element.TransformToAncestor(frame).TransformBounds(new Rect(element.RenderSize));

            if (!Contains(new Rect(frame.RenderSize), bounds))
            {
                yield return $"{label} reaches outside {Describe(frame)} ({bounds:0} in {frame.RenderSize.Width:0}×{frame.RenderSize.Height:0})";
            }
        }
    }

    private static IEnumerable<(FrameworkElement Element, FrameworkElement Frame)> VisibleDescendantsWithFrame(
        DependencyObject root, FrameworkElement frame)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (VisualTreeHelper.GetChild(root, i) is not FrameworkElement { IsVisible: true } child)
            {
                continue;
            }

            // Adorners (the focused field's caret) decorate; they are not laid out content.
            if (child is AdornerLayer)
            {
                continue;
            }

            // The content a ScrollContentPresenter hosts scrolls: its own extent is the frame.
            var childFrame = root is ScrollContentPresenter ? child : frame;

            yield return (child, childFrame);

            foreach (var nested in VisibleDescendantsWithFrame(child, childFrame))
            {
                yield return nested;
            }
        }
    }

    private static bool Contains(Rect outer, Rect inner) =>
        inner.Left >= outer.Left - 0.5 && inner.Top >= outer.Top - 0.5
        && inner.Right <= outer.Right + 0.5 && inner.Bottom <= outer.Bottom + 0.5;
}
