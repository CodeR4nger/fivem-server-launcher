using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views
{
    // Visual glue only: drag-to-reorder (measures which row and which slot, then hands the
    // move to the view model, whose gate decides), the row slide after a move, the ⋮ menu's
    // placement and keyboard opening, and the detail's slide-in on a selection change. Every
    // animation here checks the motion switch and snaps when it is off.
    public partial class ServersPage : UserControl
    {
        public ServersPage()
        {
            InitializeComponent();
            DataContextChanged += ServersPage_DataContextChanged;
            Loaded += ServersPage_Loaded;
            Unloaded += ServersPage_Unloaded;

            // While a row is dragged the PAGE holds the mouse capture, not the list: a list
            // with capture drag-selects every row the pointer enters. Captured input targets
            // the page only, so these forward it to the drag handlers.
            PreviewMouseMove += (sender, e) =>
            {
                if (IsMouseCaptured)
                {
                    SavedServersList_MouseMove(sender, e);
                }
            };
            PreviewMouseLeftButtonUp += (sender, e) =>
            {
                if (IsMouseCaptured)
                {
                    SavedServersList_PreviewMouseLeftButtonUp(sender, e);
                }
            };
            // LostMouseCapture bubbles: the list losing its own capture to the page at drag
            // start arrives here too. Only the page's own loss (Alt+Tab, ...) ends a drag.
            LostMouseCapture += (_, e) =>
            {
                if (_rowDragging && ReferenceEquals(e.OriginalSource, this))
                {
                    EndDragVisuals();
                }
            };
        }

        private void ServersPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ((INotifyCollectionChanged)oldViewModel.SavedServers).CollectionChanged -= SavedServers_CollectionChanged;
            }

            if (e.NewValue is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                ((INotifyCollectionChanged)viewModel.SavedServers).CollectionChanged += SavedServers_CollectionChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedServer))
            {
                SlideInDetail();
            }
        }

        // ------------------------- detail slide-in -------------------------

        private SavedServerItem? _shownServer;

        private void SlideInDetail()
        {
            var selected = (DataContext as MainViewModel)?.SelectedServer;

            // The view model re-announces an unchanged selection (a refused deselect):
            // only a real change slides.
            if (ReferenceEquals(selected, _shownServer))
            {
                return;
            }

            _shownServer = selected;

            if (selected is null || !Motion.GetIsEnabled(this) || !IsVisible)
            {
                return;
            }

            ((Storyboard)FindResource("MotionSlideIn")).Begin(DetailContent);
        }

        // ------------------------- the ⋮ actions menu -------------------------

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenActionsMenu();
        }

        // A press outside the open menu closes it (capture loss) before the press reaches
        // any element, so a press on ⋮ would reopen it at once. While the menu is open, the
        // window's next press is the one that dismisses it: on ⋮ it is swallowed (a toggle,
        // not a flicker). Esc and item clicks raise Closed; deactivation covers clicks into
        // other apps. Event-ordered — no time window.
        private bool _actionsMenuOpen;
        private Window? _window;

        private void ActionsMenu_Opened(object sender, RoutedEventArgs e) => _actionsMenuOpen = true;

        private void ActionsMenu_Closed(object sender, RoutedEventArgs e) => _actionsMenuOpen = false;

        private void ServersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _window = Window.GetWindow(this);

            if (_window is not null)
            {
                _window.PreviewMouseDown += Window_PreviewMouseDown;
                _window.Deactivated += Window_Deactivated;
            }
        }

        private void ServersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_window is not null)
            {
                _window.PreviewMouseDown -= Window_PreviewMouseDown;
                _window.Deactivated -= Window_Deactivated;
                _window = null;
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e) => _actionsMenuOpen = false;

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_actionsMenuOpen)
            {
                return;
            }

            _actionsMenuOpen = false;
            var point = e.GetPosition(ActionsButton);

            if (point.X >= 0 && point.Y >= 0 && point.X <= ActionsButton.ActualWidth && point.Y <= ActionsButton.ActualHeight)
            {
                e.Handled = true;
            }
        }

        // The keyboard context-menu gesture (Apps / Shift+F10) on the list opens the selected
        // server's ⋮ menu; Esc closes it (ContextMenu) and focus returns to the list. A
        // right-click (cursor position known) opens nothing: rows have no menu of their own.
        private void SavedServersList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;

            if (e.CursorLeft < 0 && e.CursorTop < 0)
            {
                Dispatcher.BeginInvoke(new Action(OpenActionsMenu), DispatcherPriority.Input);
            }
        }

        private void OpenActionsMenu()
        {
            if ((DataContext as MainViewModel)?.SelectedServer is null)
            {
                return;
            }

            ActionsMenu.PlacementTarget = ActionsButton;
            ActionsMenu.Placement = PlacementMode.Custom;
            ActionsMenu.CustomPopupPlacementCallback = BelowRightAligned;
            ActionsMenu.IsOpen = true;
        }

        // Under the ⋮ button, right edges aligned: the button sits at the pane's right edge.
        private static CustomPopupPlacement[] BelowRightAligned(Size popupSize, Size targetSize, Point offset)
        {
            return
            [
                new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, targetSize.Height + 4), PopupPrimaryAxis.Horizontal),
                new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width, -popupSize.Height - 4), PopupPrimaryAxis.Horizontal)
            ];
        }

        // ------------------- row slide after a move (FLIP) -------------------

        private void SavedServers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // A move slides its rows into their new slots: the offsets are index-derived, so
            // the slide can only start once the layout pass has placed the containers at
            // their final slots. Any other structural change snaps an in-flight slide so no
            // stale transform survives a container rebuild.
            if (e.Action == NotifyCollectionChangedAction.Move
                && e.OldStartingIndex >= 0
                && e.NewStartingIndex >= 0)
            {
                var from = e.OldStartingIndex;
                var to = e.NewStartingIndex;
                Dispatcher.BeginInvoke(new Action(() => BeginRowSlide(from, to)), DispatcherPriority.Loaded);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(SnapActiveSlides), DispatcherPriority.Loaded);
            }
        }

        private readonly HashSet<FrameworkElement> _slidingRows = new();

        private void BeginRowSlide(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex
                || !Motion.GetIsEnabled(this)
                || DataContext is not MainViewModel viewModel
                || viewModel.SavedServers.Count <= Math.Max(fromIndex, toIndex))
            {
                return;
            }

            // A rapid follow-up move snaps the previous slide to its end first, so the new
            // offsets stay index-derived and transforms never stack.
            SnapActiveSlides();

            var movedContainer = ContainerForRow(viewModel.SavedServers[toIndex]);

            if (movedContainer is null || movedContainer.ActualHeight <= 0)
            {
                return;
            }

            var rowHeight = movedContainer.ActualHeight;

            // The moved row travels |to - from| slots against the move direction.
            SlideRow(movedContainer, (fromIndex - toIndex) * rowHeight);

            // Every row it displaced shifts exactly one slot toward the vacated position. In
            // the post-move ordering those rows sit at to+1..from when the move went up, and
            // at from..to-1 when it went down.
            var (first, last) = fromIndex > toIndex
                ? (toIndex + 1, fromIndex)
                : (fromIndex, toIndex - 1);

            for (var i = first; i <= last; i++)
            {
                if (ContainerForRow(viewModel.SavedServers[i]) is { } displaced)
                {
                    SlideRow(displaced, fromIndex < toIndex ? rowHeight : -rowHeight);
                }
            }
        }

        private FrameworkElement? ContainerForRow(SavedServerItem row)
        {
            return SavedServersList.ItemContainerGenerator.ContainerFromItem(row) as FrameworkElement;
        }

        private void SlideRow(FrameworkElement container, double fromOffset)
        {
            // Base transform stays at zero: the offset lives only in the animation's From,
            // so releasing the clock can never revert the row to a stale position.
            var transform = new TranslateTransform();
            container.RenderTransform = transform;
            _slidingRows.Add(container);

            var slide = new DoubleAnimation(fromOffset, 0, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            slide.Completed += (_, _) =>
            {
                _slidingRows.Remove(container);
                transform.BeginAnimation(TranslateTransform.YProperty, null);
                container.RenderTransform = null;
            };

            transform.BeginAnimation(TranslateTransform.YProperty, slide);
        }

        private void SnapActiveSlides()
        {
            foreach (var container in _slidingRows)
            {
                if (container.RenderTransform is TranslateTransform transform)
                {
                    transform.BeginAnimation(TranslateTransform.YProperty, null);
                }

                container.RenderTransform = null;
            }

            _slidingRows.Clear();
        }

        private void SavedServersList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // A scroll invalidates the slide offsets: cancel the motion, the rows are laid
            // out at their final slots anyway.
            SnapActiveSlides();

            if (_rowDragging)
            {
                UpdateDropIndicator(_lastPointer);
            }
        }

        // ------------------------- drag-to-reorder -------------------------

        private Point _dragStart;
        private Point _lastPointer;
        private SavedServerItem? _draggedRow;
        private FrameworkElement? _draggedContainer;
        private bool _rowDragging;
        private int? _dropInsertionIndex;

        private void SavedServersList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rowDragging = false;
            _draggedRow = null;
            _draggedContainer = null;
            _dropInsertionIndex = null;

            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not { DataContext: SavedServerItem item } row
                || DataContext is not MainViewModel viewModel
                || !viewModel.IsReorderAvailable)
            {
                return;
            }

            _draggedRow = item;
            _draggedContainer = row;
            _dragStart = e.GetPosition(SavedServersList);
        }

        private void SavedServersList_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedRow is null)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                // The button came up outside our capture: forget the drag.
                EndDragVisuals();
                return;
            }

            var position = e.GetPosition(SavedServersList);

            if (!_rowDragging)
            {
                // A plain click is selection, not a drag: ignore movement until the pointer
                // leaves the system drag threshold.
                if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                    && Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _rowDragging = true;
                SnapActiveSlides();
                CaptureMouse();
                Cursor = Cursors.SizeAll;

                if (_draggedContainer is not null)
                {
                    _draggedContainer.Opacity = 0.55;
                }
            }

            UpdateDropIndicator(position);

        }

        private void UpdateDropIndicator(Point listPoint)
        {
            if (DataContext is not MainViewModel viewModel)
            {
                _dropInsertionIndex = null;
                DropIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            _lastPointer = listPoint;

            var pointer = SavedServersList.TranslatePoint(listPoint, DropIndicatorCanvas);
            var container = FindAncestor<ListBoxItem>(
                SavedServersList.InputHitTest(listPoint) as DependencyObject);

            int? insertion = null;
            double? lineY = null;

            if (container?.DataContext is SavedServerItem targetRow)
            {
                var index = viewModel.SavedServers.IndexOf(targetRow);

                if (index >= 0)
                {
                    var topLeft = container.TranslatePoint(new Point(0, 0), DropIndicatorCanvas);
                    var lowerHalf = pointer.Y > topLeft.Y + container.ActualHeight / 2;
                    insertion = lowerHalf ? index + 1 : index;
                    lineY = lowerHalf ? topLeft.Y + container.ActualHeight : topLeft.Y;
                }
            }
            else if (viewModel.SavedServers.Count > 0
                     && SavedServersList.ItemContainerGenerator
                            .ContainerFromIndex(viewModel.SavedServers.Count - 1) is FrameworkElement last)
            {
                // Below the last row: drop at the end.
                var topLeft = last.TranslatePoint(new Point(0, 0), DropIndicatorCanvas);

                if (pointer.Y > topLeft.Y + last.ActualHeight)
                {
                    insertion = viewModel.SavedServers.Count;
                    lineY = topLeft.Y + last.ActualHeight;
                }
            }

            _dropInsertionIndex = insertion;

            if (insertion is null || lineY is null)
            {
                DropIndicator.Visibility = Visibility.Collapsed;
            }
            else
            {
                Canvas.SetTop(DropIndicator, lineY.Value - 1);
                DropIndicator.Visibility = Visibility.Visible;
            }
        }

        private void SavedServersList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var wasDragging = _rowDragging;
            var dragged = _draggedRow;
            var insertion = _dropInsertionIndex;

            EndDragVisuals();

            if (!wasDragging || dragged is null || insertion is null
                || DataContext is not MainViewModel viewModel)
            {
                return;
            }

            // The drop line is an insertion point; the view model's move speaks in final
            // positions — past the dragged row the target slot shifts by one.
            var currentIndex = viewModel.SavedServers.IndexOf(dragged);

            if (currentIndex >= 0)
            {
                var finalIndex = insertion.Value > currentIndex ? insertion.Value - 1 : insertion.Value;
                viewModel.MoveServerToIndex(dragged, finalIndex);
            }
        }

        private void EndDragVisuals()
        {
            // Restore the dragged row's dim BEFORE dropping the reference.
            if (_draggedContainer is not null)
            {
                _draggedContainer.Opacity = 1;
            }

            _draggedRow = null;
            _draggedContainer = null;
            _rowDragging = false;
            _dropInsertionIndex = null;

            DropIndicator.Visibility = Visibility.Collapsed;
            Cursor = null;

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }

        private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
        {
            while (node is not null && node is not T)
            {
                node = VisualTreeHelper.GetParent(node);
            }

            return node as T;
        }
    }
}
