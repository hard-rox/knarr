namespace Knarr.App.Services;

/// <summary>Theme preference the user can choose from the menu.</summary>
public enum AppTheme
{
    /// <summary>Follow the operating system setting.</summary>
    System,

    /// <summary>Force the light variant.</summary>
    Light,

    /// <summary>Force the dark variant.</summary>
    Dark,
}

/// <summary>
/// Applies a <see cref="AppTheme"/> to the running application. Kept behind an
/// interface so view models remain UI-framework agnostic and unit-testable.
/// </summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void SetTheme(AppTheme theme);
}
