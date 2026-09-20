using System.Windows;
using System.Windows.Controls;

namespace FiveMServerLauncher.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
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

        private void FiveMOption_Click(object sender, RoutedEventArgs e)
        {
            FiveMButton.Content = "FiveM";

            FiveMDropdown.Visibility = Visibility.Collapsed;
            ArrowText.Text = "▼";
        }

        private void FiveMEnhancedOption_Click(object sender, RoutedEventArgs e)
        {
            FiveMButton.Content = "FiveM Enhanced";

            FiveMDropdown.Visibility = Visibility.Collapsed;
            ArrowText.Text = "▼";
        }
    }
}
