using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Knarr.App.Controls;
using Knarr.App.Models;

namespace Knarr.App.Converters;

/// <summary>
/// Maps a domain <see cref="ContainerStatus"/> onto the UI-layer <see cref="PillStatus"/>
/// so the Models layer stays free of any UI dependency.
/// </summary>
public sealed class ContainerStatusToPillStatusConverter : IValueConverter
{
    public static readonly ContainerStatusToPillStatusConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ContainerStatus status
            ? status switch
            {
                ContainerStatus.Running => PillStatus.Running,
                ContainerStatus.Paused => PillStatus.Paused,
                ContainerStatus.Exited => PillStatus.Stopped,
                ContainerStatus.Created => PillStatus.Neutral,
                _ => PillStatus.Neutral
            }
            : PillStatus.Neutral;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
