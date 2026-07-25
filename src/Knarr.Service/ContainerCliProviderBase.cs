using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using Knarr.Service.Exceptions;

namespace Knarr.Service;

/// <summary>
/// Shared implementation for <see cref="IContainerCliProvider"/> backends. Owns process execution,
/// streaming, cancellation, and result formatting so each concrete provider only supplies its
/// executable name, platform label, the handful of command verbs that differ between CLIs, and its
/// JSON parsing/mapping. Every method still maps 1:1 onto a single CLI invocation.
/// </summary>
internal abstract partial class ContainerCliProviderBase(ILogger logger) : IContainerCliProvider
{
    protected const string EmDash = "\u2014";

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The CLI executable name (e.g. <c>container</c> or <c>wslc</c>).</summary>
    protected abstract string Executable { get; }

    /// <summary>Human-readable platform label reported by <see cref="GetPlatformInfoAsync"/>.</summary>
    protected abstract string PlatformName { get; }

    /// <summary>Command segment that removes a container (e.g. <c>["delete"]</c> or <c>["remove"]</c>).</summary>
    protected abstract string[] RemoveContainerCommand { get; }

    /// <summary>Command segment that lists images (e.g. <c>["image", "list"]</c> or <c>["images"]</c>).</summary>
    protected abstract string[] ListImagesCommand { get; }

    /// <summary>Command segment that pulls an image (e.g. <c>["image", "pull"]</c> or <c>["pull"]</c>).</summary>
    protected abstract string[] PullImageCommand { get; }

    /// <summary>Command segment that removes an image (e.g. <c>["image", "delete"]</c> or <c>["rmi"]</c>).</summary>
    protected abstract string[] RemoveImageCommand { get; }

    /// <summary>Parses the <c>list --all --format JSON</c> payload into shaped containers.</summary>
    protected abstract IReadOnlyList<Container> ParseContainersCore(string json);

    /// <summary>Parses the image-list JSON payload into shaped images.</summary>
    protected abstract IReadOnlyList<ContainerImage> ParseImagesCore(string json);

    public async Task<IReadOnlyList<Container>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var json = await RunAsync(cancellationToken, "list", "--all", "--format", "json").ConfigureAwait(false);
        return ParseContainersCore(json);
    }

    public Task StartContainerAsync(string id, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "start", id);

    public Task StopContainerAsync(string id, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "stop", id);

    public async Task RestartContainerAsync(string id, CancellationToken cancellationToken = default)
    {
        await StopContainerAsync(id, cancellationToken).ConfigureAwait(false);
        await StartContainerAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, [.. RemoveContainerCommand, "--force", id])
            : RunAsync(cancellationToken, [.. RemoveContainerCommand, id]);

    public Task StartContainersAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
        => RunBatchLoopAsync(id => StartContainerAsync(id, cancellationToken), ids);

    public Task StopContainersAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
        => ids.Count == 0
            ? Task.CompletedTask
            : RunAsync(cancellationToken, ["stop", .. ids]);

    public Task RemoveContainersAsync(IReadOnlyList<string> ids, bool force = false, CancellationToken cancellationToken = default)
        => ids.Count == 0
            ? Task.CompletedTask
            : force
                ? RunAsync(cancellationToken, [.. RemoveContainerCommand, "--force", .. ids])
                : RunAsync(cancellationToken, [.. RemoveContainerCommand, .. ids]);

    public async Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default)
    {
        var json = await RunAsync(cancellationToken, [.. ListImagesCommand, "--format", "json"]).ConfigureAwait(false);
        return ParseImagesCore(json);
    }

    public Task PullImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, [.. PullImageCommand, reference]);

    public IAsyncEnumerable<CliOutputLine> PullImageStreamingAsync(string reference, CancellationToken cancellationToken = default)
        => RunStreamingAsync(cancellationToken, [.. PullImageCommand, reference]);

    public Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, [.. RemoveImageCommand, "--force", reference])
            : RunAsync(cancellationToken, [.. RemoveImageCommand, reference]);

    public Task RemoveImagesAsync(IReadOnlyList<string> references, bool force = false, CancellationToken cancellationToken = default)
        => references.Count == 0
            ? Task.CompletedTask
            : force
                ? RunAsync(cancellationToken, [.. RemoveImageCommand, "--force", .. references])
                : RunAsync(cancellationToken, [.. RemoveImageCommand, .. references]);

    public async Task<PlatformInfo> GetPlatformInfoAsync(CancellationToken cancellationToken = default)
    {
        var (version, reachable) = await ProbeVersionAsync(cancellationToken).ConfigureAwait(false);
        return new PlatformInfo
        {
            PlatformName = PlatformName,
            CliName = Executable,
            CliVersion = version,
            IsCliReachable = reachable,
        };
    }

    private async Task<(string Version, bool Reachable)> ProbeVersionAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Executing CLI command: {Command} --version", Executable);
        try
        {
            BufferedCommandResult result = await Cli.Wrap(Executable)
                .WithArguments(["--version"])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("CLI probe failed: {Command} --version (exit {ExitCode})", Executable, result.ExitCode);
                return ("not detected", false);
            }

            return (ParseVersion(result.StandardOutput), true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "CLI probe threw: {Command} --version", Executable);
            return ("not detected", false);
        }
    }

    private async Task<string> RunAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        var command = $"{Executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command: {Command}", command);

        BufferedCommandResult result = await Cli.Wrap(Executable)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            logger.LogDebug("CLI command succeeded: {Command}", command);
            return result.StandardOutput;
        }

        logger.LogError("CLI command failed: {Command} (exit {ExitCode}): {Error}", command, result.ExitCode, result.StandardError);
        throw new CliCommandException(command, result.ExitCode, result.StandardError);
    }

    /// <summary>
    /// Runs a CLI command as a live event stream, translating each CliWrap event into a
    /// CliWrap-agnostic <see cref="CliOutputLine"/>. The exact command line is emitted first, then
    /// stdout/stderr lines in arrival order, then a terminating exit line. Cancellation is
    /// cooperative: <paramref name="cancellationToken"/> requests a graceful interrupt, with a
    /// forceful kill scheduled as a fallback so a stuck process cannot hang indefinitely.
    /// </summary>
    private async IAsyncEnumerable<CliOutputLine> RunStreamingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params string[] arguments)
    {
        var commandLine = $"{Executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command (streaming): {Command}", commandLine);
        yield return CliOutputLine.ForCommand(commandLine);

        using CancellationTokenSource forcefulCts = new();
        // When the caller cancels, request a graceful stop and schedule a forceful kill as a fallback.
        await using CancellationTokenRegistration link = cancellationToken.Register(
            () => forcefulCts.CancelAfter(TimeSpan.FromSeconds(3)));

        Command command = Cli.Wrap(Executable)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None);

        await foreach (CommandEvent line in command.ListenAsync(Encoding.UTF8, Encoding.UTF8, forcefulCts.Token, cancellationToken))
        {
            switch (line)
            {
                case StartedCommandEvent stdOut:
                    yield return CliOutputLine.ForStandardOutput($"Started process {stdOut.ProcessId}");
                    break;
                case StandardOutputCommandEvent stdOut:
                    yield return CliOutputLine.ForStandardOutput(stdOut.Text);
                    break;
                case StandardErrorCommandEvent stdErr:
                    yield return CliOutputLine.ForStandardError(stdErr.Text);
                    break;
                case ExitedCommandEvent exited:
                    yield return CliOutputLine.ForExit(exited.ExitCode);
                    break;
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every id, collecting any <see cref="CliCommandException"/>
    /// so a single failing item does not abort the batch. Aggregated failures surface as a
    /// <see cref="AggregateCliCommandException"/>.
    /// </summary>
    private static async Task RunBatchLoopAsync(Func<string, Task> action, IReadOnlyList<string> ids)
    {
        List<CliCommandException> failures = [];
        foreach (var id in ids)
        {
            try
            {
                await action(id).ConfigureAwait(false);
            }
            catch (CliCommandException ex)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateCliCommandException(failures);
        }
    }

    protected static string ShortenId(string id) => id.Length > 12 ? id[..12] : id;

    protected static string StripDigestAlgorithm(string id)
    {
        var colon = id.IndexOf(':');
        return colon >= 0 ? id[(colon + 1)..] : id;
    }

    protected static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return EmDash;
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size.ToString(size >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture)} {units[unit]}";
    }

    private static string ParseVersion(string output)
    {
        Match match = VersionRegex().Match(output);
        if (match.Success)
        {
            return $"v{match.Value}";
        }

        var firstLine = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrEmpty(firstLine) ? "not detected" : firstLine;
    }

    [GeneratedRegex(@"\d+\.\d+\.\d+(\.\d+)?")]
    private static partial Regex VersionRegex();
}
