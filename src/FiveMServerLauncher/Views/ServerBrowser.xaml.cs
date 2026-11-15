using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views
{
    public partial class ServerBrowser : UserControl
    {
        public ServerBrowser()
        {
            InitializeComponent();
            DataContextChanged += ServerBrowser_DataContextChanged;
        }

        private void ServerBrowser_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsServerBrowserOpen)
                && sender is MainViewModel viewModel)
            {
                BrowserOpenStateChanged(viewModel.IsServerBrowserOpen);
            }
        }

        // ------------------- browser full-width slide -------------------

        private int _browserSlideGeneration;
        private bool _browserClosing;

        private TranslateTransform BrowserSlideTransform =>
            (TranslateTransform)BrowserOverlay.RenderTransform;

        // The browser pushes in and out across the full window width — a real
        // navigation slide, not a nudge. The travel distance is the live
        // window width, which a style storyboard cannot animate (style
        // storyboards freeze their animations, so their To cannot bind), so
        // the motion lives here as visual glue, gated on the inherited
        // Motion.IsEnabled switch (off: the overlay snaps in and out). The VM flag
        // stays the single truth; only visibility, the transform and hit-testing
        // are touched.
        private void BrowserOpenStateChanged(bool open)
        {
            if (open)
            {
                BrowserSlideIn();
            }
            else
            {
                BrowserSlideOut();
            }
        }

        private void BrowserSlideIn()
        {
            var resuming = _browserClosing;

            ++_browserSlideGeneration;
            _browserClosing = false;
            BrowserOverlay.IsHitTestVisible = true;
            BrowserOverlay.Visibility = Visibility.Visible;

            if (!Motion.GetIsEnabled(this))
            {
                // Motion off: the browser is simply there.
                BrowserSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
                BrowserSlideTransform.X = 0;
                return;
            }

            if (!resuming)
            {
                BrowserSlideTransform.X = -ActualWidth;
            }

            // To-only: a reopen mid-close resumes from the live position.
            var slide = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            BrowserSlideTransform.BeginAnimation(TranslateTransform.XProperty, slide);
        }

        private void BrowserSlideOut()
        {
            if (BrowserOverlay.Visibility != Visibility.Visible || _browserClosing)
            {
                return;
            }

            var generation = ++_browserSlideGeneration;

            if (!Motion.GetIsEnabled(this))
            {
                BrowserSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
                BrowserSlideTransform.X = 0;
                BrowserOverlay.IsHitTestVisible = false;
                BrowserOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            _browserClosing = true;
            BrowserOverlay.IsHitTestVisible = false;

            // To-only: closing from mid-slide-in reverses smoothly instead of
            // snapping back to the edge first.
            var slide = new DoubleAnimation(-ActualWidth, TimeSpan.FromSeconds(0.3))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            slide.Completed += (_, _) =>
            {
                if (generation != _browserSlideGeneration)
                {
                    return;
                }

                BrowserSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
                BrowserSlideTransform.X = 0;
                BrowserOverlay.Visibility = Visibility.Collapsed;
                _browserClosing = false;
            };

            BrowserSlideTransform.BeginAnimation(TranslateTransform.XProperty, slide);
        }
    }
}
