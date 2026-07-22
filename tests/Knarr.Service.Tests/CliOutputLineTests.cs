using Knarr.Service.Models;

namespace Knarr.Service.Tests;

/// <summary>
/// Unit tests for the CliWrap-agnostic <see cref="CliOutputLine"/> contract used to stream command
/// transcripts to the UI. The process-spawning stream itself is delegated to CliWrap and not
/// exercised here; these cover the factory helpers that shape each streamed line.
/// </summary>
public class CliOutputLineTests
{
    [Fact]
    public void ForCommand_SetsCommandKind()
    {
        CliOutputLine line = CliOutputLine.ForCommand("container image pull alpine");

        Assert.Equal(CliOutputKind.Command, line.Kind);
        Assert.Equal("container image pull alpine", line.Text);
        Assert.Null(line.ExitCode);
    }

    [Fact]
    public void ForStandardOutput_SetsStandardOutputKind()
    {
        CliOutputLine line = CliOutputLine.ForStandardOutput("Downloading layers...");

        Assert.Equal(CliOutputKind.StandardOutput, line.Kind);
        Assert.Equal("Downloading layers...", line.Text);
        Assert.Null(line.ExitCode);
    }

    [Fact]
    public void ForStandardError_SetsStandardErrorKind()
    {
        CliOutputLine line = CliOutputLine.ForStandardError("manifest not found");

        Assert.Equal(CliOutputKind.StandardError, line.Kind);
        Assert.Equal("manifest not found", line.Text);
        Assert.Null(line.ExitCode);
    }

    [Fact]
    public void ForExit_CarriesExitCode()
    {
        CliOutputLine line = CliOutputLine.ForExit(137);

        Assert.Equal(CliOutputKind.Exit, line.Kind);
        Assert.Equal(137, line.ExitCode);
        Assert.Contains("137", line.Text);
    }
}
