using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Knarr.Service.Models;

namespace Knarr.App.Converters;

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
            && app.TryGetResource(key, app.ActualThemeVariant, out object? resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }
}
