using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private void DropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (FiveMDropdown.Visibility == Visibility.Collapsed)
            {
                FiveMDropdown.Visibility = Visibility.Visible;
                ArrowText.Text = "▲";
            }
            else
            {
                FiveMDropdown.Visibility = Visibility.Collapsed;
                ArrowText.Text = "▼";
            }
        }

        private void ClientOption_Click(object sender, RoutedEventArgs e)
        {
            FiveMDropdown.Visibility = Visibility.Collapsed;
            ArrowText.Text = "▼";
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
            Dispatcher.BeginInvoke(new Action(RefreshMoveArrows), DispatcherPriority.Loaded);
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
