using System.Windows;

namespace FiveMServerLauncher.Views;

// Thin adapter: the real window behind the tested TrayWindowController.
public sealed class WpfTrayWindow(Window window) : ITrayWindow
{
    public bool IsVisible => window.IsVisible;

    public bool IsActive => window.IsActive;

    public WindowState WindowState
    {
        get => window.WindowState;
        set => window.WindowState = value;
    }

    public void Hide() => window.Hide();

    public void Show() => window.Show();

    public void Activate() => window.Activate();
}
