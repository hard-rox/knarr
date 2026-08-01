using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Knarr.App.Converters;

// {StaticResource}/{DynamicResource} expect a literal resource key, and {Binding} would only yield
// the key string rather than the Geometry that PathIcon.Data expects; Avalonia has no built-in way
// to bind a resource key expression, so this converter performs the key-to-resource lookup at runtime.
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public static readonly IconKeyToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key
            && Application.Current is { } app
            && app.TryGetResource(key, app.ActualThemeVariant, out object? resource)
            && resource is Geometry geometry)
        {
            return geometry;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
