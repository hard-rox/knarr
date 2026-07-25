namespace Knarr.Service.Exceptions;

/// <summary>
/// Thrown when a container CLI command exits with a non-zero status. Carries the failing command
/// line and the CLI's stderr so the UI can surface exactly what went wrong.
/// </summary>
public sealed class CliCommandException : Exception
{
    public CliCommandException(string command, int exitCode, string standardError)
        : base(BuildMessage(command, exitCode, standardError))
    {
        Command = command;
        ExitCode = exitCode;
        StandardError = standardError;
    }

    public string Command { get; }

    public int ExitCode { get; }

    public string StandardError { get; }

    private static string BuildMessage(string command, int exitCode, string standardError)
    {
        var detail = string.IsNullOrWhiteSpace(standardError) ? "(no error output)" : standardError.Trim();
        return $"Command '{command}' failed with exit code {exitCode}: {detail}";
    }
}
