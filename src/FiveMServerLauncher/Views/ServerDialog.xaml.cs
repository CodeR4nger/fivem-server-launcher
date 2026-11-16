using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FiveMServerLauncher.Views
{
    public partial class ServerDialog : UserControl
    {
        public ServerDialog()
        {
            InitializeComponent();
        }

        // The local-connection section appears below the requirement switches while the
        // address is typed; at the minimum size it starts below the scrolled body's fold,
        // so scroll it into view once it is laid out (the address field stays in view).
        private void LoopbackSection_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && sender is FrameworkElement section)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => section.BringIntoView());
            }
        }
    }
}
