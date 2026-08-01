namespace Knarr.Service.Models;

public enum CliOutputKind
{
    Command,

    StandardOutput,

    StandardError,

    Exit,
}

public sealed record CliOutputLine(CliOutputKind Kind, string Text, int? ExitCode = null)
{
    public static CliOutputLine ForCommand(string commandLine) => new(CliOutputKind.Command, commandLine);

    public static CliOutputLine ForStandardOutput(string text) => new(CliOutputKind.StandardOutput, text);

    public static CliOutputLine ForStandardError(string text) => new(CliOutputKind.StandardError, text);

    public static CliOutputLine ForExit(int exitCode) =>
        new(CliOutputKind.Exit, $"Process exited with code {exitCode}", exitCode);
}
