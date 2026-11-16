using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// Where focus goes back to when a modal closes: the element that had it when the modal
// opened, unless that element lives in a menu or popup that is gone by then — then the
// element the menu was opened from.
public class ModalFocusTests
{
    [Fact]
    public void ReturnTargetFor_APlainElement_ShouldBeTheElementItself()
    {
        // Given a focused button in the window
        var (target, button) = StaThread.Run<(object?, object)>(() =>
        {
            var button = new Button();
            return (ModalFocus.ReturnTargetFor(button), button);
        });

        // Then focus returns to it
        Assert.Same(button, target);
    }

    [Fact]
    public void ReturnTargetFor_AContextMenuItem_ShouldBeTheMenusPlacementTarget()
    {
        // Given a menu item of a context menu opened from a button
        var (target, opener) = StaThread.Run<(object?, object)>(() =>
        {
            var opener = new Button();
            var item = new MenuItem();
            var menu = new ContextMenu { PlacementTarget = opener };
            menu.Items.Add(item);
            return (ModalFocus.ReturnTargetFor(item), opener);
        });

        // Then focus returns to the button the menu came from
        Assert.Same(opener, target);
    }

    [Fact]
    public void ReturnTargetFor_AnElementInAPopup_ShouldBeThePopupsPlacementTarget()
    {
        // Given a button inside a popup placed at another button
        var (target, opener) = StaThread.Run<(object?, object)>(() =>
        {
            var opener = new Button();
            var inside = new Button();
            _ = new Popup { PlacementTarget = opener, Child = inside };
            return (ModalFocus.ReturnTargetFor(inside), opener);
        });

        // Then focus returns to that button
        Assert.Same(opener, target);
    }

    [Fact]
    public void ReturnTargetFor_Nothing_ShouldBeNothing()
    {
        // Given no focused element
        var target = StaThread.Run(() => ModalFocus.ReturnTargetFor(null));

        // Then there is nothing to return to
        Assert.Null(target);
    }
}
