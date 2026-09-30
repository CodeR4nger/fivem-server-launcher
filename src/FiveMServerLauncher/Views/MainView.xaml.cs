using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            DataContextChanged += MainView_DataContextChanged;
        }

        private bool _dropdownClosing;
        private int _dropdownHeightGeneration;

        private void DropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (FiveMDropdown.Visibility == Visibility.Collapsed || _dropdownClosing)
            {
                OpenDropdown();
                ArrowText.Text = "▲";
            }
            else
            {
                CloseDropdown();
                ArrowText.Text = "▼";
            }
        }

        private void ClientOption_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            ArrowText.Text = "▼";
        }

        // The client menu lives in the layout (not a popup), so opening and
        // closing it grow and shrink its grid slot: both motions animate the
        // menu's Height with the content clipped to the sweeping border — it
        // unrolls up out of the button seam on open and collapses back into
        // it on close, the lower area gliding along instead of jumping. Pure
        // visual glue: only visibility and height are touched.
        private void OpenDropdown()
        {
            _dropdownClosing = false;
            var generation = ++_dropdownHeightGeneration;

            if (FiveMDropdown.Visibility == Visibility.Collapsed)
            {
                FiveMDropdown.Height = 0;
            }

            FiveMDropdown.Visibility = Visibility.Visible;

            // Measure the child explicitly with an infinite constraint: the
            // explicit Height (0) clamps the child's measure constraint
            // (FrameworkElement.MeasureCore), so a layout-pass DesiredSize
            // would read 0 and the expand would animate to the border only.
            FiveMDropdown.Child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var target = FiveMDropdown.Child.DesiredSize.Height
                         + FiveMDropdown.BorderThickness.Top
                         + FiveMDropdown.BorderThickness.Bottom;

            var expand = new DoubleAnimation(target, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            expand.Completed += (_, _) =>
            {
                if (generation != _dropdownHeightGeneration)
                {
                    return;
                }

                FiveMDropdown.BeginAnimation(FrameworkElement.HeightProperty, null);
                FiveMDropdown.Height = double.NaN;
            };

            FiveMDropdown.BeginAnimation(FrameworkElement.HeightProperty, expand);
        }

        private void CloseDropdown()
        {
            if (FiveMDropdown.Visibility != Visibility.Visible || _dropdownClosing)
            {
                return;
            }

            _dropdownClosing = true;
            var generation = ++_dropdownHeightGeneration;

            // Pin the current rendered height as the animation origin: after a
            // completed expand the local Height is NaN (released to auto), and
            // a To-only DoubleAnimation throws when its origin value is NaN.
            // The motion itself is To-only, so a close fired mid-expand still
            // starts from the live animated height and reverses smoothly.
            FiveMDropdown.Height = FiveMDropdown.ActualHeight;

            var collapse = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            collapse.Completed += (_, _) =>
            {
                if (generation != _dropdownHeightGeneration)
                {
                    return;
                }

                FiveMDropdown.BeginAnimation(FrameworkElement.HeightProperty, null);
                FiveMDropdown.Height = double.NaN;
                FiveMDropdown.Visibility = Visibility.Collapsed;
                _dropdownClosing = false;
            };

            FiveMDropdown.BeginAnimation(FrameworkElement.HeightProperty, collapse);
        }

        // ============================================================
        // Reorder visuals: a gutter overlay (the move arrows) tracking
        // the hovered/selected row, and drag-to-reorder with an accent
        // drop line. All of this is visual glue: it measures the list
        // (which row, which slot) and delegates to the view model —
        // the move, its boundaries and its "not while filtering" gate
        // live and are tested there.
        // ============================================================

        private SavedServerItem? _hoveredRow;
        private SavedServerItem? _trackedRow;
        private bool _overArrows;

        private void MainView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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
                RefreshMoveArrows();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // A search filter shows a non-contiguous subset, so the reorder affordances
            // hide for as long as it is active.
            if (e.PropertyName == nameof(MainViewModel.IsReorderAvailable))
            {
                RefreshMoveArrows();
            }
        }

        // The overlay tracks rows by reference; after any structural change the
        // containers re-layout, so the reposition waits for the layout pass.
        private void SavedServers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // A move slides its rows into their new slots (FLIP): the offsets
            // are index-derived, so the slide can only start once the layout
            // pass has placed the containers at their final slots — and it
            // must land before the next render. Any other structural change
            // snaps an in-flight slide so no stale transform survives a
            // container rebuild.
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

            Dispatcher.BeginInvoke(new Action(RefreshMoveArrows), DispatcherPriority.Loaded);
        }

        // ------------------- row slide (FLIP) -------------------

        private readonly HashSet<FrameworkElement> _slidingRows = new();

        private void BeginRowSlide(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex
                || DataContext is not MainViewModel viewModel
                || viewModel.SavedServers.Count <= Math.Max(fromIndex, toIndex))
            {
                return;
            }

            // A rapid follow-up move snaps the previous slide to its end first,
            // so the new offsets stay index-derived and transforms never stack.
            SnapActiveSlides();

            var movedContainer = ContainerForRow(viewModel.SavedServers[toIndex]);

            if (movedContainer is null || movedContainer.ActualHeight <= 0)
            {
                return;
            }

            var rowHeight = movedContainer.ActualHeight;

            // The moved row travels |to - from| slots against the move direction.
            SlideRow(movedContainer, (fromIndex - toIndex) * rowHeight);

            // Every row it displaced shifts exactly one slot toward the vacated
            // position. In the post-move ordering those rows now sit at
            // to+1..from when the move went up, and at from..to-1 when it went
            // down — the moved row itself (at `to`) is never in that range.
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
            // Base transform stays at zero: the offset lives only in the
            // animation's From, so releasing the clock can never revert the
            // row to a stale position.
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

        // The pending slide offset of a row: positioning glue (the arrows
        // overlay) subtracts it so it targets the row's final slot, never the
        // mid-slide visual position.
        private double SlideOffsetOf(FrameworkElement container)
        {
            return container.RenderTransform is TranslateTransform transform && _slidingRows.Contains(container)
                ? transform.Y
                : 0;
        }

        private void SavedServerRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_rowDragging)
            {
                return;
            }

            if (sender is ListBoxItem { DataContext: SavedServerItem row })
            {
                _hoveredRow = row;
                RefreshMoveArrows();
            }
        }

        private void SavedServerRow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_rowDragging)
            {
                return;
            }

            if (sender is ListBoxItem { DataContext: SavedServerItem row } && ReferenceEquals(row, _hoveredRow))
            {
                _hoveredRow = null;
                RefreshMoveArrows();
            }
        }

        // Leaving a row to reach the arrows crosses the gutter: the overlay keeps the
        // last tracked row while the pointer is on it, otherwise the arrows would
        // collapse mid-click before the button could be pressed.
        private void MoveArrowsCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            _overArrows = true;
            RefreshMoveArrows();
        }

        private void MoveArrowsCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _overArrows = false;
            RefreshMoveArrows();
        }

        private void SavedServersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshMoveArrows();
        }

        private void SavedServersList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // A scroll invalidates the slide offsets — cancel the motion, the
            // rows are laid out at their final slots anyway.
            SnapActiveSlides();

            if (_rowDragging)
            {
                UpdateDropIndicator(_lastPointer);
            }
            else
            {
                RefreshMoveArrows();
            }
        }

        private void RefreshMoveArrows()
        {
            if (DataContext is not MainViewModel viewModel)
            {
                MoveArrowsOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            var trackedRow = _hoveredRow ?? (_overArrows ? _trackedRow : null) ?? viewModel.SelectedServer;
            var trackedIndex = trackedRow is not null ? viewModel.SavedServers.IndexOf(trackedRow) : -1;

            if (!viewModel.IsReorderAvailable || trackedIndex < 0)
            {
                MoveArrowsOverlay.Visibility = Visibility.Collapsed;
                MoveArrowsOverlay.DataContext = null;
                return;
            }

            if (SavedServersList.ItemContainerGenerator.ContainerFromIndex(trackedIndex)
                    is not FrameworkElement container)
            {
                // The tracked row has no container right now (filtered away, or being
                // rebuilt by an edit) — a hidden arrow pair is the honest state.
                MoveArrowsOverlay.Visibility = Visibility.Collapsed;
                MoveArrowsOverlay.DataContext = null;
                return;
            }

            MoveArrowsOverlay.DataContext = trackedRow;
            _trackedRow = trackedRow;
            MoveArrowsOverlay.Visibility = Visibility.Visible;
            MoveArrowsOverlay.UpdateLayout();

            var topLeft = container.TranslatePoint(new Point(0, 0), MoveArrowsCanvas);
            topLeft.Y -= SlideOffsetOf(container);
            Canvas.SetTop(
                MoveArrowsOverlay,
                topLeft.Y + (container.ActualHeight - MoveArrowsOverlay.ActualHeight) / 2);
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

            // A press on a button (the move arrows, the edit pencil) must never arm
            // a drag.
            if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
            {
                return;
            }

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
                CancelDrag();
                return;
            }

            var position = e.GetPosition(SavedServersList);

            if (!_rowDragging)
            {
                // A plain click is selection, not a drag: ignore movement until the
                // pointer leaves the system drag threshold.
                if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                    && Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _rowDragging = true;
                SnapActiveSlides();
                SavedServersList.CaptureMouse();
                SavedServersList.Cursor = Cursors.SizeAll;

                if (_draggedContainer is not null)
                {
                    _draggedContainer.Opacity = 0.55;
                }

                RefreshMoveArrows();
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
                if (!wasDragging)
                {
                    RefreshMoveArrows();
                }

                return;
            }

            // The drop line is an insertion point; the view model's move speaks in
            // final positions — past the dragged row the target slot shifts by one.
            var currentIndex = viewModel.SavedServers.IndexOf(dragged);

            if (currentIndex >= 0)
            {
                var finalIndex = insertion.Value > currentIndex ? insertion.Value - 1 : insertion.Value;
                viewModel.MoveServerToIndex(dragged, finalIndex);
            }
        }

        private void CancelDrag()
        {
            EndDragVisuals();
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

            if (SavedServersList.IsMouseCaptured)
            {
                SavedServersList.ReleaseMouseCapture();
            }

            SavedServersList.Cursor = null;
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
