using System.Text.Json;

namespace Knarr.App.Services;

/// <summary>
/// Case-insensitive helpers for reading values out of CLI JSON output. The container CLIs are not
/// consistent about property casing across versions, so lookups tolerate any casing and gracefully
/// return null when a property is absent.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>Finds a property by name (case-insensitive) on an object element, or null.</summary>
    public static JsonElement? Property(this JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, System.StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    /// <summary>Returns the first present property (case-insensitive) from <paramref name="names"/>.</summary>
    public static JsonElement? PropertyAny(this JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = element.Property(name);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Reads a string from the first matching property, or null. Numbers are stringified.</summary>
    public static string? StringAny(this JsonElement element, params string[] names)
    {
        var value = element.PropertyAny(names);
        return value?.ValueKind switch
        {
            JsonValueKind.String => value.Value.GetString(),
            JsonValueKind.Number => value.Value.GetRawText(),
            _ => null,
        };
    }
}
