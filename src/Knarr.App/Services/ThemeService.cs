using Avalonia;
using Avalonia.Styling;

namespace Knarr.App.Services;

/// <summary>
/// Maps <see cref="AppTheme"/> onto Avalonia's <see cref="ThemeVariant"/> and applies it
/// to the current application. <see cref="AppTheme.System"/> maps to
/// <see cref="ThemeVariant.Default"/>, which follows the OS setting.
/// </summary>
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
