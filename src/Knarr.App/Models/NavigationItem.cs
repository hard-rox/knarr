namespace Knarr.App.Models;

/// <summary>
/// A single entry in the main window's left navigation sidebar.
/// </summary>
public sealed class NavigationItem
{
    public NavigationItem(string title, string icon, string? badge = null)
    {
        Title = title;
        Icon = icon;
        Badge = badge;
    }

    /// <summary>Display label, e.g. "Containers".</summary>
    public string Title { get; }

    /// <summary>Glyph shown to the left of the label.</summary>
    public string Icon { get; }

    /// <summary>Optional count/badge shown on the right; null hides it.</summary>
    public string? Badge { get; }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}
