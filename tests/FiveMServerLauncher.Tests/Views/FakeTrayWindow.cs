using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// Records every call in order so tests can pin "show before restoring the state".
internal sealed class FakeTrayWindow : ITrayWindow
{
    private WindowState _windowState = WindowState.Normal;

    public List<string> Calls { get; } = [];

    public bool IsVisible { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public WindowState WindowState
    {
        get => _windowState;
        set
        {
            _windowState = value;
            Calls.Add($"State:{value}");
        }
    }

    // A user action (title-bar button, taskbar) changes the state without going through
    // the controller: set it silently, then the test raises OnStateChanged.
    public void UserSetsState(WindowState state) => _windowState = state;

    public void Hide()
    {
        IsVisible = false;
        IsActive = false;
        Calls.Add("Hide");
    }

    public void Show()
    {
        IsVisible = true;
        Calls.Add("Show");
    }

    public void Activate()
    {
        IsActive = true;
        Calls.Add("Activate");
    }
}
