using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Knarr.Service.Models;

namespace Knarr.App.Converters;

/// <summary>
/// Maps a <see cref="CliOutputKind"/> to the brush used to render that line in a terminal panel.
/// Command and exit lines are dimmed, standard error is emphasised, standard output uses the
/// primary text colour. Brushes are resolved from the active theme's resource dictionary so the
/// panel stays legible in both Light and Dark variants.
/// </summary>
public sealed class CliOutputKindToBrushConverter : IValueConverter
{
    public static readonly CliOutputKindToBrushConverter Instance = new();

    private static readonly IBrush _errorBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x35, 0x2B));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        CliOutputKind kind = value as CliOutputKind? ?? CliOutputKind.StandardOutput;

        return kind switch
        {
            CliOutputKind.StandardError => _errorBrush,
            CliOutputKind.Command => ResolveBrush("TextDimBrush"),
            CliOutputKind.Exit => ResolveBrush("TextDimBrush"),
            _ => ResolveBrush("TextBrush"),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush ResolveBrush(string key)
    {
        if (Application.Current is { } app
            && app.TryGetResource(key, app.ActualThemeVariant, out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }
}
