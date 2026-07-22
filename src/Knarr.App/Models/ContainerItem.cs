namespace Knarr.App.Models;

using System;
using System.Diagnostics.CodeAnalysis;
using Controls;
using Knarr.Service.Models;

/// <summary>
/// A single container row shown in the Containers feature. Wraps a service <see cref="Container"/>
/// record and adds UI-only concerns such as selection state and display mapping.
/// </summary>
public sealed partial class ContainerItem : ObservableObject
{
    /// <summary>Whether the row is currently ticked for a bulk (multiselect) action.</summary>
    [ObservableProperty]
    private bool _isSelected;

    public ContainerItem()
    {
    }

    [SetsRequiredMembers]
    public ContainerItem(Container container)
    {
        Name = container.Name;
        Id = container.Id;
        Image = container.Image;
        Status = container.State;
        Ports = container.Ports;
        Uptime = FormatUptime(container.State, container.StateChangedAt);
    }

    public required string Name { get; init; }

    public required string Id { get; init; }

    public required string Image { get; init; }

    public ContainerState Status { get; init; }

    public string Ports { get; init; } = "\u2014";

    public string Uptime { get; init; } = "\u2014";

    public bool IsRunning => Status == ContainerState.Running;

    /// <summary>Short 12-character id, matching the CLI's abbreviated form.</summary>
    public string ShortId => Id.Length > 12 ? Id[..12] : Id;

    public string StatusText => Status switch
    {
        ContainerState.Running => "Running",
        ContainerState.Paused => "Paused",
        ContainerState.Created => "Created",
        ContainerState.Exited => "Exited",
        _ => "Unknown"
    };

    public PillStatus PillStatus => Status switch
    {
        ContainerState.Running => PillStatus.Running,
        ContainerState.Paused => PillStatus.Paused,
        ContainerState.Exited => PillStatus.Stopped,
        _ => PillStatus.Neutral
    };

    private static string FormatUptime(ContainerState state, DateTimeOffset stateChangedAt)
    {
        if (state != ContainerState.Running || stateChangedAt == default)
        {
            return "\u2014";
        }

        TimeSpan elapsed = DateTimeOffset.UtcNow - stateChangedAt;
        if (elapsed < TimeSpan.Zero)
        {
            return "\u2014";
        }

        if (elapsed.TotalDays >= 1)
        {
            return $"{(int)elapsed.TotalDays}d {elapsed.Hours}h";
        }

        return elapsed.TotalHours >= 1
            ? $"{elapsed.Hours}h {elapsed.Minutes}m"
            : $"{elapsed.Minutes}m";
    }
}
