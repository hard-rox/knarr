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

namespace Knarr.Service.WslcCli;

internal sealed partial class WslcCliProvider(ILogger<WslcCliProvider> logger) : IContainerCliProvider
{
    private const string _executable = "wslc";
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
            ? RunAsync(cancellationToken, "remove", "--force", id)
            : RunAsync(cancellationToken, "remove", id);

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
                ? RunAsync(cancellationToken, ["remove", "--force", .. ids])
                : RunAsync(cancellationToken, ["remove", .. ids]);

    public async Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default)
    {
        var json = await RunAsync(cancellationToken, "images", "--format", "json").ConfigureAwait(false);
        return ParseImages(json);
    }

    public Task PullImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "pull", reference);

    public IAsyncEnumerable<CliOutputLine> PullImageStreamingAsync(string reference, CancellationToken cancellationToken = default)
        => RunStreamingAsync(cancellationToken, "pull", reference);

    public Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "rmi", "--force", reference)
            : RunAsync(cancellationToken, "rmi", reference);

    public Task RemoveImagesAsync(IReadOnlyList<string> references, bool force = false, CancellationToken cancellationToken = default)
        => references.Count == 0
            ? Task.CompletedTask
            : force
                ? RunAsync(cancellationToken, ["rmi", "--force", .. references])
                : RunAsync(cancellationToken, ["rmi", .. references]);

    public async Task<PlatformInfo> GetPlatformInfoAsync(CancellationToken cancellationToken = default)
    {
        var (version, reachable) = await ProbeVersionAsync(cancellationToken).ConfigureAwait(false);
        return new PlatformInfo
        {
            PlatformName = "Windows",
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
        List<ContainerResponse> responses = JsonSerializer.Deserialize<List<ContainerResponse>>(json, _jsonOptions) ?? [];
        return responses.Select(MapContainer).ToList();
    }

    internal static IReadOnlyList<ContainerImage> ParseImages(string json)
    {
        List<ImageResponse> responses = JsonSerializer.Deserialize<List<ImageResponse>>(json, _jsonOptions) ?? [];
        return responses.Select(MapImage).ToList();
    }

    private static Container MapContainer(ContainerResponse response) => new()
    {
        Id = ShortenId(response.Id),
        Name = response.Name,
        Image = response.Image,
        State = MapState(response.State),
        Ports = FormatPorts(response.Ports),
        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(response.CreatedAt),
        StateChangedAt = DateTimeOffset.FromUnixTimeSeconds(response.StateChangedAt),
    };

    private static ContainerImage MapImage(ImageResponse response) => new()
    {
        Repository = response.Repository,
        Tag = response.Tag,
        Id = ShortenId(StripDigestAlgorithm(response.Id)),
        Created = DateTimeOffset.FromUnixTimeSeconds(response.Created),
        Size = FormatSize(response.Size),
    };

    private static ContainerState MapState(int state) => state switch
    {
        2 => ContainerState.Running,
        3 => ContainerState.Exited,
        _ => ContainerState.Unknown,
    };

    private static string FormatPorts(IReadOnlyList<PortResponse>? ports)
    {
        if (ports is null || ports.Count == 0)
        {
            return _emDash;
        }

        return string.Join('\n', ports.Select(p => $"{p.HostPort}\u2192{p.ContainerPort}/{FormatProtocol(p.Protocol)}"));
    }

    private static string FormatProtocol(int protocol) => protocol switch
    {
        6 => "tcp",
        17 => "udp",
        _ => protocol.ToString(CultureInfo.InvariantCulture),
    };

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
