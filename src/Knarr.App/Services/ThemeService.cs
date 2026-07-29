using Avalonia;
using Avalonia.Styling;

namespace Knarr.App.Services;

public sealed class ThemeService : IThemeService
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;

        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }
}
