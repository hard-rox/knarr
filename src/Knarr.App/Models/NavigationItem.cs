using System;

namespace Knarr.App.Models;

/// <summary>
/// A single entry in the main window's left navigation sidebar.
/// </summary>
public sealed class NavigationItem
{
    public NavigationItem(
        string title,
        string icon,
        string? badge = null,
        Func<ViewModelBase>? createPage = null)
    {
        Title = title;
        Icon = icon;
        Badge = badge;
        CreatePage = createPage;
    }

    /// <summary>Display label, e.g. "Containers".</summary>
    public string Title { get; }

    /// <summary>Resource key of the icon geometry shown to the left of the label (e.g. "cube_regular").</summary>
    public string Icon { get; }

    /// <summary>Optional count/badge shown on the right; null hides it.</summary>
    public string? Badge { get; }

    /// <summary>
    /// Factory that produces the page view model shown when this item is selected;
    /// null for items that do not yet have a page.
    /// </summary>
    public Func<ViewModelBase>? CreatePage { get; }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}
