namespace Knarr.App.Models;

public sealed partial class NavigationItem(
    string title,
    string icon,
    string? badge = null,
    Func<ViewModelBase>? createPage = null)
    : ObservableObject
{
    public string Title { get; } = title;

    public string Icon { get; } = icon;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBadge))]
    private string? _badge = badge;

    public Func<ViewModelBase>? CreatePage { get; } = createPage;

    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}
