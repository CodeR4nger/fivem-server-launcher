using System.Windows;

namespace FiveMServerLauncher.Views;

/// <summary>The slice of a WPF <see cref="Window"/> the tray decisions need.</summary>
public interface ITrayWindow
{
    bool IsVisible { get; }

    bool IsActive { get; }

    WindowState WindowState { get; set; }

    void Hide();

    void Show();

    void Activate();
}

/// <summary>
/// The minimize-to-tray and restore decisions (the native icon glue only forwards clicks
/// here). Hidden windows are always shown through WPF before their state changes, so
/// WPF's own visibility never drifts from the real window.
/// </summary>
public sealed class TrayWindowController
{
    private readonly ITrayWindow _window;
    private readonly Func<bool> _minimizeToTray;
    private WindowState _restoreState;

    public TrayWindowController(ITrayWindow window, Func<bool> minimizeToTray)
    {
        _window = window;
        _minimizeToTray = minimizeToTray;
        _restoreState = RestorableOrNormal(window.WindowState);
    }

    /// <summary>The state a minimized window comes back to (Normal or Maximized).</summary>
    public WindowState RestoreState => _restoreState;

    // A hidden (tray) window is away even when WPF still reports it active: the
    // enrichment loop parks until the window is back in front.
    public bool IsAttended => _window.IsVisible
                              && _window.IsActive
                              && _window.WindowState != WindowState.Minimized;

    public void OnStateChanged()
    {
        var state = _window.WindowState;

        if (state != WindowState.Minimized)
        {
            _restoreState = state;
        }
        else if (_window.IsVisible && _minimizeToTray())
        {
            _window.Hide();
        }
    }

    public void ShowAndActivate()
    {
        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = _restoreState;
        }

        _window.Activate();
    }

    private static WindowState RestorableOrNormal(WindowState state) =>
        state == WindowState.Minimized ? WindowState.Normal : state;
}
