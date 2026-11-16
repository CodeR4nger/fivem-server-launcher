using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views
{
    // Thin glue: opens the client menu from the ▾ part (click, Enter/Space, Alt+Down),
    // puts keyboard focus on the current client, and hands focus back to the ▾ part when
    // the menu closes from the keyboard. Picking rides the item Command (the view model).
    public partial class OpenClientButton : UserControl
    {
        // The press that dismisses the open menu is delivered on to the ▾ button, whose
        // Click arrives on mouse-up, so a click this soon after a close is that same
        // press and must not reopen the menu (the saved-row kebab's guard). ContextMenu.Closed
        // is not raised for outside-click dismissals, so IsOpen itself is watched.
        private const long ReopenGuardMs = 400;

        private readonly ContextMenu _menu;
        private long _menuClosedAt;

        public OpenClientButton()
        {
            InitializeComponent();

            _menu = (ContextMenu)ButtonRow.Resources["ClientMenu"];
            _menu.PlacementTarget = ButtonRow;
            _menu.Opened += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, FocusCurrentClient);
            DependencyPropertyDescriptor.FromProperty(ContextMenu.IsOpenProperty, typeof(ContextMenu))
                .AddValueChanged(_menu, Menu_IsOpenChanged);
        }

        public bool IsMenuOpen => _menu.IsOpen;

        public void OpenMenu()
        {
            if (!ChooserButton.IsEnabled || _menu.IsOpen)
            {
                return;
            }

            _menu.IsOpen = true;
        }

        private void ChooserButton_Click(object sender, RoutedEventArgs e)
        {
            if (Environment.TickCount64 - _menuClosedAt < ReopenGuardMs)
            {
                return;
            }

            OpenMenu();
        }

        private void OpenClientButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.Down && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                OpenMenu();
                e.Handled = true;
            }
        }

        private void FocusCurrentClient()
        {
            var current = (DataContext as MainViewModel)?.PreferredClientOption;
            var container = current is null ? null : _menu.ItemContainerGenerator.ContainerFromItem(current);
            container ??= _menu.ItemContainerGenerator.ContainerFromIndex(0);
            (container as MenuItem)?.Focus();
        }

        private void Menu_IsOpenChanged(object? sender, EventArgs e)
        {
            if (_menu.IsOpen)
            {
                return;
            }

            _menuClosedAt = Environment.TickCount64;

            // A keyboard close (Esc, Enter on an item) leaves focus in the closed menu or
            // nowhere: give it back to the ▾ part. A click elsewhere keeps its own focus.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (Keyboard.FocusedElement is null
                    || (Keyboard.FocusedElement is DependencyObject focused && IsInMenu(focused)))
                {
                    ChooserButton.Focus();
                }
            });
        }

        private bool IsInMenu(DependencyObject element)
        {
            return ReferenceEquals(element, _menu)
                   || (element is MenuItem item && ItemsControl.ItemsControlFromItemContainer(item) == _menu);
        }
    }
}
