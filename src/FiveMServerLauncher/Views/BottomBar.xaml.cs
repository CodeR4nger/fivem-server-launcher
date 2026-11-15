using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views
{
    public partial class BottomBar : UserControl
    {
        public BottomBar()
        {
            InitializeComponent();
            DataContextChanged += BottomBar_DataContextChanged;
        }

        private TranslateTransform HighlightTransform => (TranslateTransform)NavHighlight.RenderTransform;

        private void BottomBar_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                MoveHighlight(viewModel.CurrentPage, animate: false);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentPage) && sender is MainViewModel viewModel)
            {
                MoveHighlight(viewModel.CurrentPage, animate: true);
            }
        }

        // Visual glue: the VM's CurrentPage is the single truth; the highlight's offset is
        // the tab index times the fixed tab width. A style storyboard cannot bind its To,
        // so the slide lives here, gated on the inherited Motion.IsEnabled switch (off: the
        // highlight snaps). To-only, so a switch mid-slide continues from where it is.
        private void MoveHighlight(AppPage page, bool animate)
        {
            var target = (int)page * (double)FindResource("NavTabWidthValue");

            if (!animate || !Motion.GetIsEnabled(this))
            {
                HighlightTransform.BeginAnimation(TranslateTransform.XProperty, null);
                HighlightTransform.X = target;
                return;
            }

            var slide = new DoubleAnimation(target, (Duration)FindResource("MotionMedium"))
            {
                EasingFunction = (IEasingFunction)FindResource("MotionEase")
            };

            HighlightTransform.BeginAnimation(TranslateTransform.XProperty, slide);
        }
    }
}
