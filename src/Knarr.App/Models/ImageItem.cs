namespace Knarr.App.Models;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// A single image row shown in the Images feature. UI-agnostic domain data that maps directly
/// onto fields surfaced by the container / wslc CLIs.
/// </summary>
public sealed partial class ImageItem : ObservableObject
{
    /// <summary>Whether the row is currently ticked for a bulk (multiselect) action.</summary>
    [ObservableProperty]
    private bool _isSelected;

    public required string Repository { get; init; }

    public required string Tag { get; init; }

    public required string Id { get; init; }

    public string Created { get; init; } = "\u2014";

    public string Size { get; init; } = "\u2014";

    /// <summary>Repository and tag joined for display, e.g. "nginx:latest".</summary>
    public string RepoTag => $"{Repository}:{Tag}";

    /// <summary>Abbreviated image id, matching the CLI's shortened "sha256:xxxxxx" form.</summary>
    public string ShortId => Id.Length > 19 ? $"{Id[..19]}\u2026" : Id;
}
