using System.ComponentModel;
using System.Windows;

namespace FiveMServerLauncher.Service;

// Real ISystemAnimationSetting over SystemParameters.ClientAreaAnimation (Windows' "Show
// animations in Windows"). WPF refreshes SystemParameters on WM_SETTINGCHANGE and raises
// StaticPropertyChanged on the UI thread, so following a live change is free. Thin glue,
// untested (the view model is tested through the seam).
public sealed class SystemAnimationSetting : ISystemAnimationSetting
{
    public SystemAnimationSetting()
    {
        SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
    }

    public bool IsEnabled => SystemParameters.ClientAreaAnimation;

    public event EventHandler? Changed;

    private void OnSystemParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(SystemParameters.ClientAreaAnimation))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
