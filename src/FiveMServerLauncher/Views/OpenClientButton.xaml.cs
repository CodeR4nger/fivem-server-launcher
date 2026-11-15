using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FiveMServerLauncher.Views
{
    public partial class OpenClientButton : UserControl
    {
        public OpenClientButton()
        {
            InitializeComponent();
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

        // The client menu is not a popup: it sits in a zero-height Canvas above
        // the buttons, bottom-anchored, so it grows upward over the content panel.
        // Both motions animate the menu's Height with the content clipped to the
        // sweeping border — it unrolls up out of the button seam on open and
        // collapses back into it on close. Pure visual glue: only visibility and
        // height are touched.
        private void OpenDropdown()
        {
            _dropdownClosing = false;
            var generation = ++_dropdownHeightGeneration;

            if (!Motion.GetIsEnabled(this))
            {
                SnapDropdown(Visibility.Visible);
                return;
            }

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
                         + FiveMDropdown.BorderThickness.Bottom
                         + FiveMDropdown.Padding.Top
                         + FiveMDropdown.Padding.Bottom;

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

        // Motion off: no animation at all, the menu just appears or disappears at its
        // natural size (any running height animation is dropped).
        private void SnapDropdown(Visibility visibility)
        {
            FiveMDropdown.BeginAnimation(FrameworkElement.HeightProperty, null);
            FiveMDropdown.Height = double.NaN;
            FiveMDropdown.Visibility = visibility;
            _dropdownClosing = false;
        }

        private void CloseDropdown()
        {
            if (FiveMDropdown.Visibility != Visibility.Visible || _dropdownClosing)
            {
                return;
            }

            var generation = ++_dropdownHeightGeneration;

            if (!Motion.GetIsEnabled(this))
            {
                SnapDropdown(Visibility.Collapsed);
                return;
            }

            _dropdownClosing = true;

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
    }
}
