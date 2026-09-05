namespace Knarr.Service.Exceptions;

/// <summary>Raised when a CLI command succeeds but returns an empty result array.</summary>
public sealed class EmptyCliResultException(string command)
    : Exception($"Command '{command}' returned no result.")
{
    public string Command { get; } = command;
}
