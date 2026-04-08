using ActiproSoftware.UI.Avalonia.Themes;

namespace WaterOps.Resources.Helpers;

public static class Themes
{
    public static void Refresh()
    {
        if (ModernTheme.TryGetCurrent(out var modernTheme) && modernTheme.Definition is not null)
            modernTheme.RefreshResources();
    }
}
