namespace Knarr.App.Services;

public enum AppTheme
{
    System,
    Light,
    Dark,
}

/// <summary>
/// Applies a <see cref="AppTheme"/> to the running application. Kept behind an
/// interface so view models remain UI-framework agnostic and unit-testable.
/// </summary>
public interface IThemeService
{
    public AppTheme CurrentTheme { get; }

    public void SetTheme(AppTheme theme);
}
