using System.Windows;

namespace FiveMServerLauncher.Views
{
    // What every view control sets as (or merges into) its Resources: a thin dictionary
    // whose only merged dictionary is the shared Theme. The theme is parsed once per UI
    // thread and the same instance is merged by every control, so StaticResource lookups
    // resolve at each control's own parse time (a nested UserControl's BAML cannot see its
    // future parent's resources) without re-parsing the theme per control. Per thread, not
    // per process: styles and templates are thread-bound, and the layout tests build
    // windows on their own STA threads. XAML-instantiated: keep the true parameterless ctor.
    public sealed class ThemeResources : ResourceDictionary
    {
        [ThreadStatic]
        private static Theme? _shared;

        public ThemeResources()
        {
            MergedDictionaries.Add(_shared ??= new Theme());
        }
    }
}
