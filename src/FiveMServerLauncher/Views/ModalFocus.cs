using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FiveMServerLauncher.Views;

// Keyboard focus for an in-window modal card (thin glue; the open flag stays in the view
// model). Bind IsOpen to the dialog's open flag on the card:
// - open: remembers where focus was (the opener) and moves focus to the card's first tab
//   stop once it is laid out;
// - while open: focus that would leave the card for another element of the same window is
//   refused (the Tab cycle itself is KeyboardNavigation.TabNavigation="Cycle" on the card;
//   this catches mouse clicks on the title bar and anything programmatic). Popups opened
//   from inside the card (combo box lists) are other windows and are allowed;
// - close: focus goes back to the opener, or to the element its menu/popup was opened from.
public static class ModalFocus
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen",
        typeof(bool),
        typeof(ModalFocus),
        new PropertyMetadata(false, OnIsOpenChanged));

    private static readonly DependencyProperty ReturnTargetProperty = DependencyProperty.RegisterAttached(
        "ReturnTarget",
        typeof(WeakReference<DependencyObject>),
        typeof(ModalFocus));

    public static bool GetIsOpen(DependencyObject element) => (bool)element.GetValue(IsOpenProperty);

    public static void SetIsOpen(DependencyObject element, bool value) => element.SetValue(IsOpenProperty, value);

    // The element focus returns to when the modal closes: the focused element itself, or —
    // when it sits in a context menu or popup, which is gone by then — the element that
    // menu was placed at.
    public static DependencyObject? ReturnTargetFor(DependencyObject? focused)
    {
        for (var node = focused; node is not null; node = LogicalTreeHelper.GetParent(node) ?? VisualParent(node))
        {
            switch (node)
            {
                case ContextMenu menu:
                    return menu.PlacementTarget;
                case Popup popup:
                    return popup.PlacementTarget ?? LogicalTreeHelper.GetParent(popup);
            }
        }

        return focused;
    }

    private static DependencyObject? VisualParent(DependencyObject node) =>
        node is Visual ? VisualTreeHelper.GetParent(node) : null;

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement card)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            Open(card);
        }
        else
        {
            Close(card);
        }
    }

    private static void Open(FrameworkElement card)
    {
        var opener = ReturnTargetFor(Keyboard.FocusedElement as DependencyObject);
        card.SetValue(ReturnTargetProperty, opener is null ? null : new WeakReference<DependencyObject>(opener));
        card.PreviewLostKeyboardFocus += KeepFocusInside;

        // After layout: the card was collapsed until this very change.
        card.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (GetIsOpen(card) && !card.IsKeyboardFocusWithin)
            {
                card.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        });
    }

    private static void Close(FrameworkElement card)
    {
        card.PreviewLostKeyboardFocus -= KeepFocusInside;
        var stored = (WeakReference<DependencyObject>?)card.GetValue(ReturnTargetProperty);
        card.ClearValue(ReturnTargetProperty);

        // Only when focus is still the modal's (or already dropped by its collapse): a
        // close that moved focus somewhere on purpose keeps it there.
        var focused = Keyboard.FocusedElement;
        var focusIsModals = focused is null or Window
            || (focused is Visual visual && visual.IsDescendantOf(card));

        if (stored is null || !stored.TryGetTarget(out var target) || !focusIsModals)
        {
            return;
        }

        // Focus the opener, or its nearest focusable ancestor (a menu's placement target
        // may be a plain row panel).
        for (var node = target; node is not null; node = VisualParent(node))
        {
            if (node is UIElement { IsVisible: true, Focusable: true, IsEnabled: true } element && element.Focus())
            {
                return;
            }
        }
    }

    private static void KeepFocusInside(object sender, KeyboardFocusChangedEventArgs e)
    {
        var card = (FrameworkElement)sender;

        if (!GetIsOpen(card) || e.NewFocus is not DependencyObject next)
        {
            return;
        }

        var sameWindow = PresentationSource.FromDependencyObject(next) is { } source
            && ReferenceEquals(source, PresentationSource.FromVisual(card));

        if (sameWindow && next is Visual visual && !visual.IsDescendantOf(card))
        {
            e.Handled = true;
        }
    }
}
