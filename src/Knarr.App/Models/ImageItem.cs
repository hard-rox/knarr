namespace Knarr.App.Models;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Knarr.Service.Models;

/// <summary>
/// A single image row shown in the Images feature. Wraps a service <see cref="ContainerImage"/>
/// record and adds UI-only concerns such as selection state and display mapping.
/// </summary>
public sealed partial class ImageItem : ObservableObject
{
    /// <summary>Whether the row is currently ticked for a bulk (multiselect) action.</summary>
    [ObservableProperty]
    private bool _isSelected;

    public ImageItem()
    {
    }

    [SetsRequiredMembers]
    public ImageItem(ContainerImage image)
    {
        Repository = image.Repository;
        Tag = image.Tag;
        Id = image.Id;
        Created = FormatCreated(image.Created);
        Size = image.Size;
    }

    public required string Repository { get; init; }

    public required string Tag { get; init; }

    /// <summary>Short 12-character image id.</summary>
    public required string Id { get; init; }

    public string Created { get; init; } = "\u2014";

    public string Size { get; init; } = "\u2014";

    /// <summary>Repository and tag joined for display, e.g. "nginx:latest".</summary>
    public string RepoTag => $"{Repository}:{Tag}";

    private static string FormatCreated(DateTimeOffset created)
    {
        if (created == default)
        {
            return "\u2014";
        }

        TimeSpan elapsed = DateTimeOffset.UtcNow - created;
        if (elapsed < TimeSpan.Zero)
        {
            return created.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (elapsed.TotalDays >= 1)
        {
            var days = (int)elapsed.TotalDays;
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }

        if (elapsed.TotalHours >= 1)
        {
            var hours = (int)elapsed.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
        return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
    }
}
