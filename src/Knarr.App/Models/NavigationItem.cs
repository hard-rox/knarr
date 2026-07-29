namespace Knarr.App.Models;

public sealed partial class NavigationItem : ObservableObject
{
    public NavigationItem(
        string title,
        string icon,
        string? badge = null,
        Func<ViewModelBase>? createPage = null)
    {
        Title = title;
        Icon = icon;
        _badge = badge;
        CreatePage = createPage;
    }

    public string Title { get; }

    public string Icon { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBadge))]
    private string? _badge;

    public Func<ViewModelBase>? CreatePage { get; }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);
}
