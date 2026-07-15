using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Knarr.App.Converters;

/// <summary>
/// Resolves a bound icon resource key (for example, "cube_regular") to a <see cref="Geometry"/>
/// from the application's merged resource dictionaries.
///
/// Why this converter exists:
/// - <c>{StaticResource ...}</c> and <c>{DynamicResource ...}</c> expect a literal resource key.
///   <c>{DynamicResource Icon}</c> looks for a resource literally named "Icon", not the value of a bound property.
/// - <c>{Binding Icon}</c> returns the key string (for example, "cube_regular"), but <see cref="PathIcon.Data"/>
///   expects a <see cref="Geometry"/>, not a resource-key string.
/// - Avalonia does not support binding a resource key expression directly (for example, DynamicResource of Binding),
///   so this converter performs the runtime key-to-resource lookup.
/// </summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public static readonly IconKeyToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key
            && Application.Current is { } app
            && app.TryGetResource(key, app.ActualThemeVariant, out var resource)
            && resource is Geometry geometry)
        {
            return geometry;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
