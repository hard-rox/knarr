namespace Knarr.Service.Models;

/// <summary>
/// The origin of a single <see cref="CliOutputLine"/> emitted while streaming a CLI command.
/// </summary>
public enum CliOutputKind
{
    /// <summary>The exact command line being executed (emitted once, first, for transparency).</summary>
    Command,

    /// <summary>A line written by the process to its standard output stream.</summary>
    StandardOutput,

    /// <summary>A line written by the process to its standard error stream.</summary>
    StandardError,

    /// <summary>The process has exited (carries the exit code).</summary>
    Exit,
}

/// <summary>
/// One entry in a streamed CLI command transcript. Deliberately CliWrap-agnostic so the app layer
/// can consume streamed command output without taking a dependency on the process-execution library.
/// </summary>
/// <param name="Kind">Where the line originated.</param>
/// <param name="Text">The line text (for <see cref="CliOutputKind.Exit"/> a human-readable summary).</param>
/// <param name="ExitCode">The process exit code; only set when <see cref="Kind"/> is <see cref="CliOutputKind.Exit"/>.</param>
public sealed record CliOutputLine(CliOutputKind Kind, string Text, int? ExitCode = null)
{
    /// <summary>Creates a <see cref="CliOutputKind.Command"/> line carrying the exact command executed.</summary>
    public static CliOutputLine ForCommand(string commandLine) => new(CliOutputKind.Command, commandLine);

    /// <summary>Creates a <see cref="CliOutputKind.StandardOutput"/> line.</summary>
    public static CliOutputLine ForStandardOutput(string text) => new(CliOutputKind.StandardOutput, text);

    /// <summary>Creates a <see cref="CliOutputKind.StandardError"/> line.</summary>
    public static CliOutputLine ForStandardError(string text) => new(CliOutputKind.StandardError, text);

    /// <summary>Creates a <see cref="CliOutputKind.Exit"/> line carrying the process exit code.</summary>
    public static CliOutputLine ForExit(int exitCode) =>
        new(CliOutputKind.Exit, $"Process exited with code {exitCode}", exitCode);
}
