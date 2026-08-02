namespace Knarr.Service.Models;

/// <summary>A single key/value pair from an inspect payload (environment variable or label).</summary>
public sealed record InspectionEntry(string Key, string Value);
