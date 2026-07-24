using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using Knarr.Service.Exceptions;

namespace Knarr.Service.AppleContainerCli;

/// <summary>
/// <see cref="IContainerCliProvider"/> backed by Apple's first-party <c>container</c> CLI on macOS.
/// Every method maps 1:1 onto a single <c>container</c> invocation.
/// </summary>
internal sealed partial class AppleContainerCliProvider(ILogger<AppleContainerCliProvider> logger) : IContainerCliProvider
{
    private const string _executable = "container";
    private const string _emDash = "\u2014";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Container>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var json = await RunAsync(cancellationToken, "list", "--all", "--format", "json").ConfigureAwait(false);
        return ParseContainers(json);
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
            ? RunAsync(cancellationToken, "delete", "--force", id)
            : RunAsync(cancellationToken, "delete", id);

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
                ? RunAsync(cancellationToken, ["delete", "--force", .. ids])
                : RunAsync(cancellationToken, ["delete", .. ids]);

    public async Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default)
    {
        var json = await RunAsync(cancellationToken, "image", "list", "--format", "json").ConfigureAwait(false);
        return ParseImages(json);
    }

    public Task PullImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "pull", reference);

    public IAsyncEnumerable<CliOutputLine> PullImageStreamingAsync(string reference, CancellationToken cancellationToken = default)
        => RunStreamingAsync(cancellationToken, "image", "pull", reference);

    public Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "image", "delete", "--force", reference)
            : RunAsync(cancellationToken, "image", "delete", reference);

    public Task RemoveImagesAsync(IReadOnlyList<string> references, bool force = false, CancellationToken cancellationToken = default)
        => references.Count == 0
            ? Task.CompletedTask
            : force
                ? RunAsync(cancellationToken, ["image", "delete", "--force", .. references])
                : RunAsync(cancellationToken, ["image", "delete", .. references]);

    public async Task<PlatformInfo> GetPlatformInfoAsync(CancellationToken cancellationToken = default)
    {
        var (version, reachable) = await ProbeVersionAsync(cancellationToken).ConfigureAwait(false);
        return new PlatformInfo
        {
            PlatformName = "macOS",
            CliName = _executable,
            CliVersion = version,
            IsCliReachable = reachable,
        };
    }

    private async Task<(string Version, bool Reachable)> ProbeVersionAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Executing CLI command: {Command} --version", _executable);
        try
        {
            BufferedCommandResult result = await Cli.Wrap(_executable)
                .WithArguments(["--version"])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("CLI probe failed: {Command} --version (exit {ExitCode})", _executable, result.ExitCode);
                return ("not detected", false);
            }

            return (ParseVersion(result.StandardOutput), true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "CLI probe threw: {Command} --version", _executable);
            return ("not detected", false);
        }
    }

    private async Task<string> RunAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        var command = $"{_executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command: {Command}", command);

        BufferedCommandResult result = await Cli.Wrap(_executable)
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
        var commandLine = $"{_executable} {string.Join(' ', arguments)}";
        logger.LogDebug("Executing CLI command (streaming): {Command}", commandLine);
        yield return CliOutputLine.ForCommand(commandLine);

        using CancellationTokenSource forcefulCts = new();
        // When the caller cancels, request a graceful stop and schedule a forceful kill as a fallback.
        await using CancellationTokenRegistration link = cancellationToken.Register(
            () => forcefulCts.CancelAfter(TimeSpan.FromSeconds(3)));

        Command command = Cli.Wrap(_executable)
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

    internal static IReadOnlyList<Container> ParseContainers(string json)
    {
        List<AppleContainerElement> elements =
            JsonSerializer.Deserialize<List<AppleContainerElement>>(json, _jsonOptions) ?? [];
        return elements.Select(MapContainer).ToList();
    }

    internal static IReadOnlyList<ContainerImage> ParseImages(string json)
    {
        List<AppleImageElement> elements =
            JsonSerializer.Deserialize<List<AppleImageElement>>(json, _jsonOptions) ?? [];
        return elements.Select(MapImage).ToList();
    }

    private static Container MapContainer(AppleContainerElement element) => new()
    {
        // The Apple CLI uses the (user-supplied or generated) id as both id and name; keep it intact.
        Id = element.Id,
        Name = element.Id,
        Image = element.Configuration.Image.Reference,
        State = MapState(element.Status.State),
        Ports = FormatPorts(element.Configuration.PublishedPorts),
        CreatedAt = element.Configuration.CreationDate,
        StateChangedAt = element.Status.StartedDate ?? element.Configuration.CreationDate,
    };

    private static ContainerImage MapImage(AppleImageElement element)
    {
        var (repository, tag) = SplitReference(element.Configuration.Name);
        return new ContainerImage
        {
            Repository = repository,
            Tag = tag,
            Id = ShortenId(StripDigestAlgorithm(element.Id)),
            Created = element.Configuration.CreationDate,
            Size = FormatSize(element.Variants?.Sum(v => v.Size) ?? 0),
        };
    }

    private static ContainerState MapState(string state) => state.ToLowerInvariant() switch
    {
        "running" => ContainerState.Running,
        "stopped" or "exited" => ContainerState.Exited,
        "created" => ContainerState.Created,
        "paused" => ContainerState.Paused,
        _ => ContainerState.Unknown,
    };

    private static string FormatPorts(IReadOnlyList<ApplePublishedPort>? ports)
    {
        if (ports is null || ports.Count == 0)
        {
            return _emDash;
        }

        return string.Join(
            '\n',
            ports.Select(p => $"{p.HostPort}\u2192{p.ContainerPort}/{FormatProtocol(p.Proto)}"));
    }

    private static string FormatProtocol(string? protocol) =>
        string.IsNullOrWhiteSpace(protocol) ? "tcp" : protocol.ToLowerInvariant();

    private static (string Repository, string Tag) SplitReference(string reference)
    {
        var lastColon = reference.LastIndexOf(':');
        var lastSlash = reference.LastIndexOf('/');

        // A colon only denotes a tag when it appears after the final path separator; otherwise it is
        // a registry port (e.g. "localhost:5000/img") and the reference carries no explicit tag.
        return lastColon > lastSlash
            ? (reference[..lastColon], reference[(lastColon + 1)..])
            : (reference, "latest");
    }

    private static string ShortenId(string id) => id.Length > 12 ? id[..12] : id;

    private static string StripDigestAlgorithm(string id)
    {
        var colon = id.IndexOf(':');
        return colon >= 0 ? id[(colon + 1)..] : id;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return _emDash;
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
