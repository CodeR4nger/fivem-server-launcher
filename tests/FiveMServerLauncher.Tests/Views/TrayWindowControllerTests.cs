using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class TrayWindowControllerTests
{
    private static (FakeTrayWindow Window, TrayWindowController Controller) Create(bool minimizeToTray)
    {
        var window = new FakeTrayWindow();
        return (window, new TrayWindowController(window, () => minimizeToTray));
    }

    private static void Minimize(FakeTrayWindow window, TrayWindowController controller)
    {
        window.UserSetsState(WindowState.Minimized);
        controller.OnStateChanged();
    }

    [Fact]
    public void OnStateChanged_WhenMinimizedWithMinimizeToTrayOn_ShouldHideTheWindow()
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);

        // When
        Minimize(window, controller);

        // Then
        Assert.Equal(["Hide"], window.Calls);
        Assert.False(window.IsVisible);
    }

    [Fact]
    public void OnStateChanged_WhenMinimizedWithMinimizeToTrayOff_ShouldLeaveItOnTheTaskbar()
    {
        // Given
        var (window, controller) = Create(minimizeToTray: false);

        // When
        Minimize(window, controller);

        // Then
        Assert.Empty(window.Calls);
        Assert.True(window.IsVisible);
    }

    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void OnStateChanged_WhenNotMinimized_ShouldNeverHide(WindowState state)
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);

        // When
        window.UserSetsState(state);
        controller.OnStateChanged();

        // Then
        Assert.Empty(window.Calls);
    }

    [Fact]
    public void OnStateChanged_WhenAlreadyHidden_ShouldNotHideAgain()
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);
        Minimize(window, controller);
        window.Calls.Clear();

        // When
        controller.OnStateChanged();

        // Then
        Assert.Empty(window.Calls);
    }

    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void ShowAndActivate_AfterHidingToTray_ShouldShowThenRestoreThePreviousStateThenActivate(WindowState previous)
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);
        window.UserSetsState(previous);
        controller.OnStateChanged();
        Minimize(window, controller);
        window.Calls.Clear();

        // When
        controller.ShowAndActivate();

        // Then — the state is never changed on a hidden window.
        Assert.Equal(["Show", $"State:{previous}", "Activate"], window.Calls);
        Assert.True(controller.IsAttended);
    }

    [Fact]
    public void ShowAndActivate_WhenMinimizedOnTheTaskbar_ShouldRestoreWithoutShowing()
    {
        // Given — setting off: a plain minimize.
        var (window, controller) = Create(minimizeToTray: false);
        window.UserSetsState(WindowState.Maximized);
        controller.OnStateChanged();
        Minimize(window, controller);

        // When
        controller.ShowAndActivate();

        // Then
        Assert.Equal(["State:Maximized", "Activate"], window.Calls);
    }

    [Fact]
    public void ShowAndActivate_WhenAlreadyShown_ShouldOnlyActivate()
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);
        window.IsActive = false;

        // When
        controller.ShowAndActivate();

        // Then
        Assert.Equal(["Activate"], window.Calls);
    }

    [Fact]
    public void ShowAndActivate_WithoutAnyRememberedState_ShouldRestoreToNormal()
    {
        // Given — the first state change ever seen is the minimize.
        var (window, controller) = Create(minimizeToTray: true);
        Minimize(window, controller);
        window.Calls.Clear();

        // When
        controller.ShowAndActivate();

        // Then
        Assert.Equal(["Show", "State:Normal", "Activate"], window.Calls);
    }

    [Fact]
    public void ShowAndActivate_WhenTheWindowStartedMaximized_ShouldRestoreMaximized()
    {
        // Given — maximized before the controller existed, no state change seen yet.
        var window = new FakeTrayWindow();
        window.UserSetsState(WindowState.Maximized);
        var controller = new TrayWindowController(window, () => true);
        Minimize(window, controller);
        window.Calls.Clear();

        // When
        controller.ShowAndActivate();

        // Then
        Assert.Equal(["Show", "State:Maximized", "Activate"], window.Calls);
    }

    [Theory]
    [InlineData(true, true, WindowState.Normal, true)]
    [InlineData(true, true, WindowState.Maximized, true)]
    [InlineData(true, true, WindowState.Minimized, false)]
    [InlineData(true, false, WindowState.Normal, false)]
    [InlineData(false, false, WindowState.Normal, false)]
    [InlineData(false, true, WindowState.Normal, false)]
    public void IsAttended_ShouldRequireVisibleActiveAndNotMinimized(
        bool visible, bool active, WindowState state, bool expected)
    {
        // Given — a hidden (tray) window counts as away even if WPF still reports it active.
        var window = new FakeTrayWindow { IsVisible = visible, IsActive = active };
        window.UserSetsState(state);
        var controller = new TrayWindowController(window, () => true);

        // When / Then
        Assert.Equal(expected, controller.IsAttended);
    }

    [Fact]
    public void IsAttended_AfterHidingToTrayAndShowingAgain_ShouldGoAwayThenBack()
    {
        // Given
        var (window, controller) = Create(minimizeToTray: true);
        Assert.True(controller.IsAttended);

        // When
        Minimize(window, controller);

        // Then
        Assert.False(controller.IsAttended);

        // When
        controller.ShowAndActivate();

        // Then
        Assert.True(controller.IsAttended);
    }

    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void RestoreState_WhenMinimized_ShouldBeTheStateBeforeTheMinimize(WindowState before)
    {
        // Given
        var (window, controller) = Create(minimizeToTray: false);
        window.UserSetsState(before);
        controller.OnStateChanged();

        // When
        Minimize(window, controller);

        // Then — what the window placement saves as "maximized" for a minimized window
        Assert.Equal(before, controller.RestoreState);
    }
}
