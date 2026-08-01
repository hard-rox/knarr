namespace Knarr.Service.Models;

public sealed record PlatformInfo
{
    public required string PlatformName { get; init; }

    public required string CliName { get; init; }

    public required string CliVersion { get; init; }

    public bool IsCliReachable { get; init; }
}
