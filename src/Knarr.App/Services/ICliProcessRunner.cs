using System.Threading;
using System.Threading.Tasks;

namespace Knarr.App.Services;

/// <summary>Result of running a CLI process: its exit code and captured output streams.</summary>
public readonly record struct CliProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Runs an external CLI executable and captures its output. Isolating process invocation behind
/// an interface keeps the container CLI providers unit-testable without spawning real processes.
/// </summary>
public interface ICliProcessRunner
{
    /// <summary>
    /// Runs <paramref name="executable"/> with the supplied arguments and returns its exit code
    /// and captured stdout/stderr. Arguments are passed as a list so each is escaped individually.
    /// </summary>
    Task<CliProcessResult> RunAsync(
        string executable,
        System.Collections.Generic.IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
