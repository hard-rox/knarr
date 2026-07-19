namespace Knarr.App.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using Controls;

/// <summary>Lifecycle state of a container as reported by the underlying CLI.</summary>
public enum ContainerStatus
{
    Created,
    Running,
    Paused,
    Exited
}

/// <summary>
/// A single container row shown in the Containers feature. UI-agnostic domain data that maps
/// directly onto fields surfaced by the container / wslc CLIs.
/// </summary>
public sealed partial class ContainerItem : ObservableObject
{
    /// <summary>Whether the row is currently ticked for a bulk (multiselect) action.</summary>
    [ObservableProperty]
    private bool _isSelected;

    public required string Name { get; init; }

    public required string Id { get; init; }

    public required string Image { get; init; }

    public ContainerStatus Status { get; init; }

    public string Ports { get; init; } = "\u2014";

    public string Cpu { get; init; } = "\u2014";

    public string Memory { get; init; } = "\u2014";

    public string Uptime { get; init; } = "\u2014";

    public bool IsRunning => Status == ContainerStatus.Running;

    /// <summary>Short 12-character id, matching the CLI's abbreviated form.</summary>
    public string ShortId => Id.Length > 12 ? Id[..12] : Id;

    public string ResourceUsage => $"{Cpu} \u00b7 {Memory}";

    public string StatusText => Status switch
    {
        ContainerStatus.Running => "Running",
        ContainerStatus.Paused => "Paused",
        ContainerStatus.Created => "Created",
        ContainerStatus.Exited => "Exited (0)",
        _ => Status.ToString()
    };

    public PillStatus PillStatus => Status switch
    {
        ContainerStatus.Running => PillStatus.Running,
        ContainerStatus.Paused => PillStatus.Paused,
        ContainerStatus.Exited => PillStatus.Stopped,
        _ => PillStatus.Neutral
    };
}
